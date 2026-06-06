// Editor registry to keep track of all editor instances
// In react, when editor is invoked, via useEditor it manages the instance and its lifecycle, but in this vanilla JS implementation we need to handle it ourselves.
// editorId → {editor, dotNetRef, rootElement}
const editorRegistry = new Map();

// TipTap modules are imported via ESM.sh CDN. 
// We use a single promise to load all required modules in parallel and cache the results for subsequent editor instances.
const tiptapModulesPromise = Promise.all([
    import("https://esm.sh/@tiptap/core@2.11.5"),
    import("https://esm.sh/@tiptap/starter-kit@2.11.5"),
    import("https://esm.sh/@tiptap/extension-underline@2.11.5"),
    import("https://esm.sh/@tiptap/extension-highlight@2.11.5"),
    import("https://esm.sh/@tiptap/extension-text-align@2.11.5"),
    import("https://esm.sh/@tiptap/extension-link@2.11.5"),
    import("https://esm.sh/@tiptap/extension-image@2.11.5")
]).then(([coreModule, starterKitModule, underlineModule, highlightModule, textAlignModule, linkModule, imageModule]) => ({
    Editor: coreModule.Editor,
    StarterKit: starterKitModule.default,
    Underline: underlineModule.default,
    Highlight: highlightModule.default,
    TextAlign: textAlignModule.default,
    Link: linkModule.default,
    Image: imageModule.default
}));

// Function to build content-payload (editor content) that will be sent to Blazor when editor content changes.
// It includes HTML, JSON, and plain text representations of the content.
// const json = editor.getJSON();
// handleChange(json);
function buildContentPayload(editor) {
    return {
        html: editor.getHTML(),
        json: editor.getJSON(),
        text: editor.getText()
    };
}

// editor.isActive is used to set state of block type
// when we want to get editor state, it is returned as a single state object
function buildStatePayload(editor) {
    return {
        isBold: editor.isActive("bold"),
        isItalic: editor.isActive("italic"),
        isStrike: editor.isActive("strike"),
        isUnderline: editor.isActive("underline"),
        isHighlight: editor.isActive("highlight"),
        isLink: editor.isActive("link"),
        isParagraph: editor.isActive("paragraph"),
        isHeading1: editor.isActive("heading", { level: 1 }),
        isHeading2: editor.isActive("heading", { level: 2 }),
        isHeading3: editor.isActive("heading", { level: 3 }),
        isBulletList: editor.isActive("bulletList"),
        isOrderedList: editor.isActive("orderedList"),
        isCodeBlock: editor.isActive("codeBlock"),
        isAlignLeft: editor.isActive({ textAlign: "left" }),
        isAlignCenter: editor.isActive({ textAlign: "center" }),
        isAlignRight: editor.isActive({ textAlign: "right" }),
        isAlignJustify: editor.isActive({ textAlign: "justify" }),
        canUndo: editor.can().chain().focus().undo().run(),
        canRedo: editor.can().chain().focus().redo().run()
    };
}

// ---------------------- EDITOR STATE CONBTENT AND STATE CHANGE i.e. handleChange --------------------- //

// emulates the behavior of onChange callback in React implementation, 
// by invoking corresponding methods in Blazor via DotNetRef when editor content or state changes.
async function notifyContentChanged(entry) {
    if (!entry?.dotNetRef) {
        return;
    }

    await entry.dotNetRef
        .invokeMethodAsync("OnContentChanged", buildContentPayload(entry.editor))
        .catch(error => console.error("Failed to notify Blazor about editor content changes.", error));
}

// emulates the behavior of StateChange callback in React implementation,
// by invoking corresponding methods in Blazor via DotNetRef when editor content or state changes.
async function notifyStateChanged(entry) {
    if (!entry?.dotNetRef) {
        return;
    }

    await entry.dotNetRef
        .invokeMethodAsync("OnEditorStateChanged", buildStatePayload(entry.editor))
        .catch(error => console.error("Failed to notify Blazor about editor state changes.", error));
}


// executes the actual command which we click on toolbar, 
// by invoking corresponding chainable commands in TipTap editor instance.
function executeCommand(editor, commandName) {
    switch (commandName) {
        case "undo":
            return editor.chain().focus().undo().run();
        case "redo":
            return editor.chain().focus().redo().run();
        case "h1":
            return editor.chain().focus().toggleHeading({ level: 1 }).run();
        case "h2":
            return editor.chain().focus().toggleHeading({ level: 2 }).run();
        case "h3":
            return editor.chain().focus().toggleHeading({ level: 3 }).run();
        case "paragraph":
            return editor.chain().focus().setParagraph().run();
        case "bold":
            return editor.chain().focus().toggleBold().run();
        case "italic":
            return editor.chain().focus().toggleItalic().run();
        case "strike":
            return editor.chain().focus().toggleStrike().run();
        case "underline":
            return editor.chain().focus().toggleUnderline().run();
        case "highlight":
            return editor.chain().focus().toggleHighlight().run();
        case "alignLeft":
            return editor.chain().focus().setTextAlign("left").run();
        case "alignCenter":
            return editor.chain().focus().setTextAlign("center").run();
        case "alignRight":
            return editor.chain().focus().setTextAlign("right").run();
        case "alignJustify":
            return editor.chain().focus().setTextAlign("justify").run();
        case "bulletList":
            return editor.chain().focus().toggleBulletList().run();
        case "orderedList":
            return editor.chain().focus().toggleOrderedList().run();
        case "codeBlock":
            return editor.chain().focus().toggleCodeBlock().run();
        case "horizontalRule":
            return editor.chain().focus().setHorizontalRule().run();
        case "link": {
            const isActive = editor.isActive("link");
            if (isActive) {
                return editor.chain().focus().unsetLink().run();
            }

            const previousUrl = editor.getAttributes("link")?.href ?? "";
            const url = window.prompt("Enter URL", previousUrl);
            if (!url) {
                return false;
            }

            return editor.chain().focus().extendMarkRange("link").setLink({ href: url }).run();
        }
        case "image": {
            const url = window.prompt("Enter image URL");
            if (!url) {
                return false;
            }

            return editor.chain().focus().setImage({ src: url }).run();
        }
        default:
            console.warn(`Unsupported Tiptap command: ${commandName}`);
            return false;
    }
}

// emulates creation of the editor instance / variable via useEditor in REACT
// it is called from Blazor when editor component is initialized, and it creates the editor instance and sets up all necessary event handlers for content and state changes.
async function createEditor(editorId, element, dotNetRef, options) {

    // we need to wait for the TipTap modules to be loaded before we can create the editor instance, which is handled by the tiptapModulesPromise.
    const { Editor, StarterKit, Underline, Highlight, TextAlign, Link, Image } = await tiptapModulesPromise;

    destroyEditor(editorId);

    // Mount directly onto the element — no extra wrapper div needed
    element.innerHTML = "";

    const entry = {
        dotNetRef,
        editor: null,
        rootElement: element
    };

    const editor = new Editor({
        element: element,   // ← was: host (the injected editor-surface div)
        extensions: [
            StarterKit,
            Underline,
            Highlight,
            Link.configure({
                openOnClick: false,
                autolink: true,
                linkOnPaste: true
            }),
            Image.configure({
                inline: false,
                allowBase64: true
            }),
            TextAlign.configure({
                types: ["heading", "paragraph"]
            })
        ],
        content: options?.content ?? "",
        autofocus: options?.autofocus ?? false,
        // when editor is created, we need to notify Blazor about content and state changes, so that it can initialize the toolbar button states accordingly.
        onCreate: async () => {
            await notifyContentChanged(entry);
            await notifyStateChanged(entry);
        },
        // when keystroke / input happens, we need to notify Blazor about content and state changes, so that it can update the toolbar button states accordingly.
        onUpdate: async () => {
            await notifyContentChanged(entry);
            await notifyStateChanged(entry);
            // Scroll the cursor into view whenever content changes
            entry.editor.commands.scrollIntoView();
        },
        // when selection changes, we need to notify Blazor about state changes, so that it can update the toolbar button states accordingly.
        onSelectionUpdate: async () => {
            await notifyStateChanged(entry);
        },
        // when editor is focused, we need to notify Blazor about state changes, so that it can update the toolbar button states accordingly.
        onTransaction: async () => {
            await notifyStateChanged(entry);
        }
    });

    entry.editor = editor;
    editorRegistry.set(editorId, entry);
}

// this function is called from Blazor to register the DotNetRef for the editor instance
// which will be used to invoke callbacks to Blazor when editor content or state changes.
function registerStateListener(editorId, dotNetRef) {
    const entry = editorRegistry.get(editorId);

    if (!entry) {
        return;
    }

    entry.dotNetRef = dotNetRef;
    void notifyStateChanged(entry);
}

// execiutes the command which is triggered by toolbar button click in Blazor, by invoking corresponding chainable commands in TipTap editor instance.
function execCommand(editorId, commandName) {
    const entry = editorRegistry.get(editorId);

    if (!entry?.editor) {
        console.warn(`Editor '${editorId}' is not ready yet.`);
        return false;
    }

    const normalizedCommand = commandName.trim();
    const wasHandled = executeCommand(entry.editor, normalizedCommand);

    if (wasHandled) {
        void notifyStateChanged(entry);
    }

    return wasHandled;
}

// to destroy the editor instance and clean up resources when the editor component is unmounted in Blazor
// we need to manually destroy the editor instance and clear the registry entry, which is automatically handled by React when component unmounts.
function destroyEditor(editorId) {
    const entry = editorRegistry.get(editorId);

    if (!entry) {
        return;
    }

    if (entry.editor && !entry.editor.isDestroyed) {
        entry.editor.destroy();
    }

    if (entry.rootElement instanceof HTMLElement) {
        entry.rootElement.innerHTML = "";
    }

    editorRegistry.delete(editorId);
}

// added after with copilot
function setEditorContent(editorId, html) {
    const entry = editorRegistry.get(editorId);

    if (!entry?.editor) {
        console.warn(`Editor '${editorId}' is not ready yet.`);
        return;
    }

    entry.editor.commands.setContent(html, false);
}

function getEditorHtml(editorId) {
    const entry = editorRegistry.get(editorId);

    if (!entry?.editor) {
        console.warn(`Editor '${editorId}' is not ready yet.`);
        return "";
    }

    return entry.editor.getHTML();
}

window.createEditor = createEditor;
window.registerStateListener = registerStateListener;
window.execCommand = execCommand;
window.destroyEditor = destroyEditor;
window.setEditorContent = setEditorContent;
window.getEditorHtml = getEditorHtml;
