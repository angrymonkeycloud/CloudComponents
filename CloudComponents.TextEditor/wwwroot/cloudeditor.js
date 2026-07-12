// ES module — loaded via Blazor JS isolation:
// import('./_content/AngryMonkey.CloudComponents.Editor/cloudeditor.js')
// Implements the CloudTextEditor contenteditable engine: command execution,
// selection tracking, paste sanitization, media insertion and value sync.

const BLOCK_TAGS = ['h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'blockquote', 'pre', 'div'];

const ALLOWED_TAGS = {
    p: [], div: [], br: [], hr: [],
    h1: [], h2: [], h3: [], h4: [], h5: [], h6: [],
    strong: [], b: [], em: [], i: [], u: [], s: [], strike: [], del: [],
    sub: [], sup: [], code: [], pre: [], blockquote: [],
    ul: [], ol: ['start', 'type'], li: [],
    a: ['href', 'target', 'rel', 'title'],
    img: ['src', 'alt', 'width', 'height', 'title'],
    video: ['src', 'controls', 'width', 'height', 'poster', 'preload'],
    source: ['src', 'type'],
    figure: ['class'], figcaption: [],
    iframe: ['src', 'width', 'height', 'allow', 'allowfullscreen', 'frameborder', 'title'],
    table: [], thead: [], tbody: [], tfoot: [], tr: [], td: ['colspan', 'rowspan'], th: ['colspan', 'rowspan'],
    span: [], font: ['color']
};

const REMOVE_ENTIRELY = ['script', 'style', 'head', 'meta', 'link', 'title', 'object', 'embed', 'form', 'input', 'button', 'select', 'textarea', 'noscript'];

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

function sanitizeUrl(url) {
    const value = String(url ?? '').trim();
    if (!value) return null;
    if (/^(javascript|vbscript|data):/i.test(value.replace(/[\s\u0000-\u001F]/g, ''))) {
        return /^data:image\//i.test(value) ? value : null;
    }
    return value;
}

function sanitizeFragment(root, options) {
    const nodes = [...root.childNodes];

    for (const node of nodes) {
        if (node.nodeType === Node.COMMENT_NODE) {
            node.remove();
            continue;
        }

        if (node.nodeType !== Node.ELEMENT_NODE) continue;

        const tag = node.tagName.toLowerCase();

        if (REMOVE_ENTIRELY.includes(tag)) {
            node.remove();
            continue;
        }

        sanitizeFragment(node, options);

        const allowedAttributes = ALLOWED_TAGS[tag];

        if (allowedAttributes === undefined || (tag === 'iframe' && !options.allowIframes)) {
            // Unknown tag — unwrap, keeping children.
            while (node.firstChild) root.insertBefore(node.firstChild, node);
            node.remove();
            continue;
        }

        for (const attribute of [...node.attributes]) {
            const name = attribute.name.toLowerCase();

            if (!allowedAttributes.includes(name)) {
                node.removeAttribute(attribute.name);
                continue;
            }

            if ((name === 'href' || name === 'src') && sanitizeUrl(attribute.value) === null)
                node.removeAttribute(attribute.name);
        }

        if (tag === 'a' && node.getAttribute('target') === '_blank')
            node.setAttribute('rel', 'noopener');
    }
}

export function create(dotNetRef, editorElement, options) {
    return new CloudTextEditorController(dotNetRef, editorElement, options);
}

class CloudTextEditorController {
    #dotNetRef;
    #editor;
    #options;
    #savedRange = null;
    #changeTimer = null;
    #stateTimer = null;
    #lastNotifiedHtml = null;
    #disposed = false;

    // Bound handler references stored as regular fields so addEventListener /
    // removeEventListener receive the same function object each time, and so
    // we avoid the "Private method is not writable" error that occurs when
    // trying to rebind a private-method slot.
    #boundHandlers;

    constructor(dotNetRef, editorElement, options) {
        this.#dotNetRef = dotNetRef;
        this.#editor = editorElement;
        this.#options = Object.assign({ debounceMs: 250, sanitizePaste: true, allowIframes: true }, options ?? {});

        try { document.execCommand('defaultParagraphSeparator', false, 'p'); } catch { /* not critical */ }

        this.#boundHandlers = {
            onInput:           () => this.#onInput(),
            onPaste:           (e) => this.#onPaste(e),
            onKeyDown:         (e) => this.#onKeyDown(e),
            onFocus:           () => this.#onFocus(),
            onBlur:            () => this.#onBlur(),
            onSelectionChange: () => this.#onSelectionChange()
        };

        this.#editor.addEventListener('input',     this.#boundHandlers.onInput);
        this.#editor.addEventListener('paste',     this.#boundHandlers.onPaste);
        this.#editor.addEventListener('keydown',   this.#boundHandlers.onKeyDown);
        this.#editor.addEventListener('focus',     this.#boundHandlers.onFocus);
        this.#editor.addEventListener('blur',      this.#boundHandlers.onBlur);
        document.addEventListener('selectionchange', this.#boundHandlers.onSelectionChange);

        this.#updateEmptyState();
    }

    // ----- Content ----------------------------------------------------------

    setHtml(html) {
        this.#editor.innerHTML = html ?? '';
        this.#ensureBlock();
        this.#updateEmptyState();
        this.#lastNotifiedHtml = this.getHtml();
    }

    getHtml() {
        if (this.#isEmpty()) return '';
        return this.#editor.innerHTML;
    }

    setReadOnly(readOnly) {
        this.#editor.setAttribute('contenteditable', readOnly ? 'false' : 'true');
    }

    focus() {
        this.#editor.focus();
    }

    // ----- Commands ---------------------------------------------------------

    exec(command, value) {
        this.focus();
        this.restoreSelection();
        this.#exec(command, value ?? null);
    }

    execColor(command, color) {
        this.focus();
        this.restoreSelection();

        try { document.execCommand('styleWithCSS', false, true); } catch { /* ignore */ }
        this.#exec(command, color);
        try { document.execCommand('styleWithCSS', false, false); } catch { /* ignore */ }
    }

    formatBlock(tag) {
        this.focus();
        this.restoreSelection();
        this.#exec('formatBlock', `<${tag}>`);
    }

    toggleBlock(tag) {
        this.focus();
        this.restoreSelection();
        const current = this.#currentBlockTag();
        this.#exec('formatBlock', current === tag ? '<p>' : `<${tag}>`);
    }

    toggleInlineCode() {
        this.focus();
        this.restoreSelection();

        const selection = window.getSelection();
        if (!selection || selection.rangeCount === 0) return;

        const range = selection.getRangeAt(0);
        const codeElement = this.#closest(range.startContainer, 'code');

        if (this.#closest(range.startContainer, 'pre')) return;

        if (codeElement) {
            const parent = codeElement.parentNode;
            while (codeElement.firstChild) parent.insertBefore(codeElement.firstChild, codeElement);
            codeElement.remove();
            this.#queueChange();
        }
        else if (!range.collapsed) {
            this.#exec('insertHTML', `<code>${escapeHtml(range.toString())}</code>`);
        }
    }

    createLink(url, text, newTab) {
        const safeUrl = sanitizeUrl(url);
        if (!safeUrl) return;

        this.focus();
        this.restoreSelection();

        const selection = window.getSelection();
        const collapsed = !selection || selection.rangeCount === 0 || selection.getRangeAt(0).collapsed;
        const attributes = newTab ? ' target="_blank" rel="noopener"' : '';

        if (collapsed || (text && text.length && text !== selection.toString())) {
            const label = text && text.length ? text : safeUrl;

            if (!collapsed) this.#exec('delete', null);

            this.#exec('insertHTML', `<a href="${escapeHtml(safeUrl)}"${attributes}>${escapeHtml(label)}</a>`);
        }
        else {
            this.#exec('createLink', safeUrl);

            const anchor = this.#closest(selection.anchorNode, 'a');

            if (anchor) {
                if (newTab) { anchor.target = '_blank'; anchor.rel = 'noopener'; }
                else { anchor.removeAttribute('target'); anchor.removeAttribute('rel'); }
            }
        }
    }

    unlink() {
        this.focus();
        this.restoreSelection();

        const selection = window.getSelection();
        const anchor = selection ? this.#closest(selection.anchorNode, 'a') : null;

        if (anchor && selection.rangeCount > 0 && selection.getRangeAt(0).collapsed) {
            const range = document.createRange();
            range.selectNodeContents(anchor);
            selection.removeAllRanges();
            selection.addRange(range);
        }

        this.#exec('unlink', null);
    }

    insertHtml(html) {
        this.focus();
        this.restoreSelection();
        this.#exec('insertHTML', html);
    }

    // ----- Selection --------------------------------------------------------

    saveSelection() {
        const selection = window.getSelection();

        if (selection && selection.rangeCount > 0 && this.#editor.contains(selection.getRangeAt(0).commonAncestorContainer))
            this.#savedRange = selection.getRangeAt(0).cloneRange();

        return this.getSelectionState();
    }

    restoreSelection() {
        if (!this.#savedRange) return;

        const selection = window.getSelection();
        selection.removeAllRanges();
        selection.addRange(this.#savedRange);
    }

    getSelectionState() {
        const state = {
            bold: false, italic: false, underline: false, strikethrough: false,
            subscript: false, superscript: false, inlineCode: false,
            blockTag: 'p', alignment: 'left',
            unorderedList: false, orderedList: false,
            link: false, linkUrl: null, selectedText: ''
        };

        const selection = window.getSelection();

        if (!selection || selection.rangeCount === 0 || !this.#editor.contains(selection.anchorNode))
            return state;

        const query = command => { try { return document.queryCommandState(command); } catch { return false; } };

        state.bold = query('bold');
        state.italic = query('italic');
        state.underline = query('underline');
        state.strikethrough = query('strikeThrough');
        state.subscript = query('subscript');
        state.superscript = query('superscript');
        state.unorderedList = query('insertUnorderedList');
        state.orderedList = query('insertOrderedList');

        if (query('justifyCenter')) state.alignment = 'center';
        else if (query('justifyRight')) state.alignment = 'right';
        else if (query('justifyFull')) state.alignment = 'justify';

        state.blockTag = this.#currentBlockTag();
        state.inlineCode = !!this.#closest(selection.anchorNode, 'code') && !this.#closest(selection.anchorNode, 'pre');

        const anchor = this.#closest(selection.anchorNode, 'a');
        state.link = !!anchor;
        state.linkUrl = anchor ? anchor.getAttribute('href') : null;
        state.selectedText = selection.toString();

        return state;
    }

    dispose() {
        this.#disposed = true;
        clearTimeout(this.#changeTimer);
        clearTimeout(this.#stateTimer);
        this.#editor.removeEventListener('input',     this.#boundHandlers.onInput);
        this.#editor.removeEventListener('paste',     this.#boundHandlers.onPaste);
        this.#editor.removeEventListener('keydown',   this.#boundHandlers.onKeyDown);
        this.#editor.removeEventListener('focus',     this.#boundHandlers.onFocus);
        this.#editor.removeEventListener('blur',      this.#boundHandlers.onBlur);
        document.removeEventListener('selectionchange', this.#boundHandlers.onSelectionChange);
    }

    // ----- Internals --------------------------------------------------------

    #exec(command, value) {
        try { document.execCommand(command, false, value); }
        catch (error) { console.warn(`CloudTextEditor: command '${command}' failed.`, error); }

        this.#queueChange();
        this.#queueStatePush();
    }

    #onInput() {
        this.#updateEmptyState();
        this.#queueChange();
    }

    #onPaste(event) {
        if (!this.#options.sanitizePaste) return;

        const html = event.clipboardData?.getData('text/html');
        if (!html) return; // plain text pastes are handled natively

        event.preventDefault();

        const template = document.createElement('template');
        template.innerHTML = html;
        sanitizeFragment(template.content, this.#options);

        const container = document.createElement('div');
        container.appendChild(template.content.cloneNode(true));

        this.#exec('insertHTML', container.innerHTML);
    }

    #onKeyDown(event) {
        if (event.key === 'Tab') {
            const selection = window.getSelection();

            if (selection && this.#closest(selection.anchorNode, 'li')) {
                event.preventDefault();
                this.#exec(event.shiftKey ? 'outdent' : 'indent', null);
            }
        }
        else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault();
            this.saveSelection();
            this.#invoke('HandleLinkShortcutAsync');
        }
    }

    #onFocus() {
        this.#invoke('HandleFocusAsync');
    }

    #onBlur() {
        this.#flushChange();
        this.#invoke('HandleBlurAsync');
    }

    #onSelectionChange() {
        const selection = window.getSelection();

        if (!selection || !selection.anchorNode || !this.#editor.contains(selection.anchorNode)) return;

        if (selection.rangeCount > 0)
            this.#savedRange = selection.getRangeAt(0).cloneRange();

        this.#queueStatePush();
    }

    #queueChange() {
        clearTimeout(this.#changeTimer);
        this.#changeTimer = setTimeout(() => this.#flushChange(), this.#options.debounceMs);
    }

    #flushChange() {
        clearTimeout(this.#changeTimer);
        this.#updateEmptyState();

        const html = this.getHtml();
        if (html === this.#lastNotifiedHtml) return;

        this.#lastNotifiedHtml = html;
        this.#invoke('HandleContentChangedAsync', html);
    }

    #queueStatePush() {
        clearTimeout(this.#stateTimer);
        this.#stateTimer = setTimeout(() => this.#invoke('UpdateSelectionStateAsync', this.getSelectionState()), 100);
    }

    #invoke(method, ...args) {
        if (this.#disposed) return;
        this.#dotNetRef.invokeMethodAsync(method, ...args).catch(() => { /* circuit gone */ });
    }

    #currentBlockTag() {
        const selection = window.getSelection();

        if (!selection || !selection.anchorNode || !this.#editor.contains(selection.anchorNode)) return 'p';

        let node = selection.anchorNode;

        while (node && node !== this.#editor) {
            if (node.nodeType === Node.ELEMENT_NODE) {
                const tag = node.tagName.toLowerCase();
                if (BLOCK_TAGS.includes(tag) && tag !== 'div') return tag;
            }

            node = node.parentNode;
        }

        return 'p';
    }

    #closest(node, tag) {
        while (node && node !== this.#editor) {
            if (node.nodeType === Node.ELEMENT_NODE && node.tagName.toLowerCase() === tag)
                return node;

            node = node.parentNode;
        }

        return null;
    }

    #ensureBlock() {
        if (this.#editor.innerHTML.trim() === '')
            this.#editor.innerHTML = '<p><br></p>';
    }

    #isEmpty() {
        if (this.#editor.querySelector('img, video, iframe, hr, table')) return false;
        return (this.#editor.textContent ?? '').trim() === '';
    }

    #updateEmptyState() {
        this.#editor.classList.toggle('_empty', this.#isEmpty());
    }
}
