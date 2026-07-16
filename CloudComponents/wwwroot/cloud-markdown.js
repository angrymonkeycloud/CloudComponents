function decodedFragment(hash) {
    try {
        return decodeURIComponent(hash.startsWith("#") ? hash.slice(1) : hash);
    } catch {
        return hash.startsWith("#") ? hash.slice(1) : hash;
    }
}

function findTarget(root, fragment) {
    const candidates = [fragment];
    const withoutOrdinal = fragment.replace(/^\d+[\s.)-]+/, "");

    if (withoutOrdinal !== fragment) {
        candidates.push(withoutOrdinal);
    }

    for (const id of candidates) {
        const target = document.getElementById(id);
        if (target && root.contains(target)) {
            return target;
        }
    }

    return null;
}

function clearFragment() {
    history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);
}

function scrollToFragment(root, fragment) {
    const target = findTarget(root, decodedFragment(fragment));
    if (!target) {
        return false;
    }

    target.scrollIntoView({ behavior: "smooth", block: "start" });
    clearFragment();
    return true;
}

export function bindAndRestore(root) {
    root.querySelectorAll("a[href]").forEach(anchor => {
        if (anchor.dataset.cloudMarkdownAnchorBound === "true") {
            return;
        }

        const destination = new URL(anchor.href, window.location.href);
        const isSameDocumentAnchor = destination.origin === window.location.origin
            && destination.pathname === window.location.pathname
            && Boolean(destination.hash);

        if (!isSameDocumentAnchor) {
            return;
        }

        anchor.dataset.cloudMarkdownAnchorBound = "true";
        anchor.addEventListener("click", event => {
            if (scrollToFragment(root, destination.hash)) {
                event.preventDefault();
            }
        });
    });

    if (window.location.hash) {
        window.setTimeout(() => scrollToFragment(root, window.location.hash), 0);
    }
}
