const editorRegistry = new Map();

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

function buildContentPayload(editor) {
    return {
        html: editor.getHTML(),
        json: editor.getJSON(),
        text: editor.getText()
    };
}

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

async function notifyContentChanged(entry) {
    if (!entry?.dotNetRef) {
        return;
    }

    await entry.dotNetRef
        .invokeMethodAsync("OnContentChanged", buildContentPayload(entry.editor))
        .catch(error => console.error("Failed to notify Blazor about editor content changes.", error));
}

async function notifyStateChanged(entry) {
    if (!entry?.dotNetRef) {
        return;
    }

    await entry.dotNetRef
        .invokeMethodAsync("OnEditorStateChanged", buildStatePayload(entry.editor))
        .catch(error => console.error("Failed to notify Blazor about editor state changes.", error));
}

function createEditorHost(element) {
    element.innerHTML = "";

    const host = document.createElement("div");
    host.className = "editor-surface";
    element.appendChild(host);

    return host;
}

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

async function createEditor(editorId, element, dotNetRef, options) {
    const { Editor, StarterKit, Underline, Highlight, TextAlign, Link, Image } = await tiptapModulesPromise;

    destroyEditor(editorId);

    const host = createEditorHost(element);

    const entry = {
        dotNetRef,
        editor: null,
        rootElement: element
    };

    const editor = new Editor({
        element: host,
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
        onCreate: async () => {
            await notifyContentChanged(entry);
            await notifyStateChanged(entry);
        },
        onUpdate: async () => {
            await notifyContentChanged(entry);
            await notifyStateChanged(entry);
        },
        onSelectionUpdate: async () => {
            await notifyStateChanged(entry);
        },
        onTransaction: async () => {
            await notifyStateChanged(entry);
        }
    });

    entry.editor = editor;
    editorRegistry.set(editorId, entry);
}

function registerStateListener(editorId, dotNetRef) {
    const entry = editorRegistry.get(editorId);

    if (!entry) {
        return;
    }

    entry.dotNetRef = dotNetRef;
    void notifyStateChanged(entry);
}

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

window.createEditor = createEditor;
window.registerStateListener = registerStateListener;
window.execCommand = execCommand;
window.destroyEditor = destroyEditor;
