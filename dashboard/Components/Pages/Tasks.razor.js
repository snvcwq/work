// Custom pointer-based drag reorder — deliberately NOT native HTML5 draggable/dragover/drop
// (see prior note: native DnD's dropEffect/cursor handling fights a live-reordering UI), and
// deliberately NOT reordering real DOM nodes during the drag either. Blazor's own re-render
// assumes the DOM still matches whatever it last rendered; if JS moves nodes structurally
// (insertBefore) for live feedback, committing on top of that makes Blazor's diff — computed
// from a stale idea of "current" order — apply moves that land the row off from where the
// drag actually left it, and reverting to the pre-drag order before committing is *visible*
// (the row snaps back, then jumps again once Blazor's own patch arrives over the network).
// So: only CSS transforms move anything during the drag — dragEl follows the pointer, and
// displaced siblings preview shifting out of the way — real DOM order is untouched until
// Blazor performs the actual reorder once, on drop. Nothing needs to revert, because nothing
// structural ever changed; the FLIP observer below animates Blazor's own patch smoothly from
// wherever the preview left things (already close to correct) to the real final layout.
export function initSortable(dotNetRef) {
    const list = document.querySelector(".task-list");
    if (!list) return;
    // Marked on the element itself, not a module-level flag — Blazor's enhanced navigation
    // (the default for internal links) swaps page content via AJAX rather than a full
    // reload, so this JS module stays loaded and a module-level "already initialized" flag
    // would stay true forever after the first visit. Navigating away from /tasks and back
    // tears down and recreates the .task-list element, though, so a module-level guard was
    // silently skipping the listener setup on that *new* element on every return visit —
    // dragging looked like it "stopped working" after leaving the tab once.
    if (list.dataset.sortableBound === "true") return;
    list.dataset.sortableBound = "true";

    let dragEl = null;
    let rows = [];          // fixed snapshot of DOM order at drag start; never mutated
    let dragOriginalIndex = 0;
    let targetIndex = 0;    // dragEl's current conceptual slot among `rows`
    let dragStep = 0;       // dragEl's own height + the list's row gap
    let startY = 0;
    let currentDy = 0;
    let minDy = 0;          // how far dragEl can move up: to the top slot, no further
    let maxDy = 0;          // how far dragEl can move down: to the bottom slot, no further

    // The list scrolls with the page itself (no inner scroll container), so a long list can
    // be taller than the viewport. Without this, dragging to the top/bottom slot means
    // releasing, scrolling the page by hand, and re-grabbing the row to keep going.
    const AUTOSCROLL_EDGE = 80;      // px from the viewport edge that triggers autoscroll
    const AUTOSCROLL_MAX_SPEED = 18; // px per frame once the pointer is right at the edge
    let startScrollY = 0;
    let lastClientY = 0;
    let autoScrollFrame = null;

    // Shared by pointermove and the autoscroll loop — a scroll can shift dragEl's target
    // slot even while the pointer itself hasn't moved, so both need to recompute the same way.
    function updateDragPosition() {
        if (!dragEl) return;
        currentDy = Math.max(minDy, Math.min(maxDy, (lastClientY - startY) + (window.scrollY - startScrollY)));
        dragEl.style.transform = `translateY(${currentDy}px)`;

        // Purely arithmetic, not a live-rect crossing check — applyPreview() already
        // shifts every displaced row by exactly dragStep (dragEl's own size), not each
        // row's individual height, so the threshold for "has dragEl passed the next
        // slot" has to be computed the same way. Reading getBoundingClientRect() on a
        // sibling here used to double- or under-count crossings: those rows have a
        // 0.15s transform transition running, so a rect read moments after
        // applyPreview() sets a new transform can land mid-animation instead of at the
        // resting position, especially across several pointermove events fired in
        // quick succession during a real drag.
        const rawIndex = dragOriginalIndex + Math.round(currentDy / dragStep);
        const newTarget = Math.max(0, Math.min(rows.length - 1, rawIndex));
        if (newTarget !== targetIndex) {
            targetIndex = newTarget;
            applyPreview();
        }
    }

    // Runs continuously during a drag (not just on pointermove) — the pointer can sit dead
    // still right at the viewport edge and the page still needs to keep scrolling.
    function autoScrollStep() {
        if (!dragEl) { autoScrollFrame = null; return; }

        let speed = 0;
        if (lastClientY < AUTOSCROLL_EDGE) {
            const depth = (AUTOSCROLL_EDGE - lastClientY) / AUTOSCROLL_EDGE;
            speed = -Math.ceil(depth * AUTOSCROLL_MAX_SPEED);
        } else if (lastClientY > window.innerHeight - AUTOSCROLL_EDGE) {
            const depth = (lastClientY - (window.innerHeight - AUTOSCROLL_EDGE)) / AUTOSCROLL_EDGE;
            speed = Math.ceil(depth * AUTOSCROLL_MAX_SPEED);
        }

        if (speed !== 0) {
            const before = window.scrollY;
            window.scrollBy(0, speed);
            if (window.scrollY !== before) updateDragPosition();
        }

        autoScrollFrame = requestAnimationFrame(autoScrollStep);
    }

    // What the full order would be if `rows` were actually spliced — used as the final
    // committed order once the drag ends.
    function conceptualOrder() {
        const arr = rows.slice();
        const [el] = arr.splice(dragOriginalIndex, 1);
        arr.splice(targetIndex, 0, el);
        return arr;
    }

    // Shifts every displaced sibling by exactly dragEl's own step (not its own height) —
    // dragEl is the one sliding past them, so the gap it opens/closes is always its size,
    // regardless of the shifted rows' own heights (1-line vs 2-line titles).
    function applyPreview() {
        rows.forEach((row, i) => {
            if (row === dragEl) return;
            let shift = 0;
            if (targetIndex < dragOriginalIndex && i >= targetIndex && i < dragOriginalIndex) {
                shift = dragStep;
            } else if (targetIndex > dragOriginalIndex && i > dragOriginalIndex && i <= targetIndex) {
                shift = -dragStep;
            }
            row.style.transform = shift ? `translateY(${shift}px)` : "";
        });
    }

    list.addEventListener("pointerdown", (e) => {
        // Drag can start from anywhere on the row — the handle icon is just a visual cue,
        // not the only grabbable spot. Links, the status dropdown, the type badge, and
        // delete still need to behave like normal clicks, so they opt out.
        if (e.target.closest("a, button, .status-pill, .type-badge")) return;
        const row = e.target.closest(".task-row");
        if (!row) return;

        dragEl = row;
        rows = [...list.querySelectorAll(".task-row")];
        dragOriginalIndex = rows.indexOf(dragEl);
        targetIndex = dragOriginalIndex;

        const dragRect = dragEl.getBoundingClientRect();
        const gap = parseFloat(getComputedStyle(list).rowGap) || 0;
        dragStep = dragRect.height + gap;

        startY = e.clientY;
        currentDy = 0;
        // dragEl can only travel within the list itself — up to the first slot, down to the
        // last — never past either end into whatever sits above/below the list (page header,
        // filter bar, add-form). Clamped here once, from the fixed `rows` snapshot, rather than
        // in pointermove against live rects for the same reason applyPreview() avoids them.
        minDy = -(dragOriginalIndex * dragStep);
        maxDy = (rows.length - 1 - dragOriginalIndex) * dragStep;

        startScrollY = window.scrollY;
        lastClientY = e.clientY;
        if (autoScrollFrame === null) autoScrollFrame = requestAnimationFrame(autoScrollStep);

        rows.forEach(r => { if (r !== dragEl) r.style.transition = "transform 0.15s ease"; });
        row.style.transition = "none";
        row.setPointerCapture(e.pointerId);
        row.classList.add("task-row-dragging");
        document.body.classList.add("task-dragging-active");
        row.style.zIndex = "50";
        row.style.position = "relative";

        dotNetRef.invokeMethodAsync("NotifyDragStart");
        e.preventDefault();
    });

    list.addEventListener("pointermove", (e) => {
        if (!dragEl) return;
        lastClientY = e.clientY;
        updateDragPosition();
    });

    async function endDrag() {
        if (!dragEl) return;
        const row = dragEl;
        const finalOrder = conceptualOrder();
        dragEl = null;
        if (autoScrollFrame !== null) {
            cancelAnimationFrame(autoScrollFrame);
            autoScrollFrame = null;
        }

        row.classList.remove("task-row-dragging");
        document.body.classList.remove("task-dragging-active");
        row.style.zIndex = "";
        row.style.position = "";

        const orderedIds = finalOrder.map(el => el.dataset.taskId);

        // Deliberately NOT clearing the preview transforms here. They stay exactly as the
        // user left them — real DOM order hasn't changed, so there's nothing to revert —
        // until Blazor's own patch physically reorders the nodes; the FLIP observer clears
        // them at that point as part of measuring the true final layout. Snapshotting now
        // (while the preview is still showing) is what lets that later animation continue
        // smoothly from here instead of jumping back to the pre-drag layout first.
        armFlip();

        await dotNetRef.invokeMethodAsync("CommitReorder", orderedIds);
    }

    list.addEventListener("pointerup", endDrag);
    list.addEventListener("pointercancel", endDrag);
}

// Animates reorders that happen for reasons other than a local drag (e.g. the periodic
// poll picking up a change made via the API, or Blazor's own re-render after a commit).
// armFlip snapshots positions right before such a mutation; once the DOM settles into its
// new order, this diffs against the snapshot and animates each row from its old spot
// instead of letting it jump.
let pendingRects = null;

export function armFlip() {
    const rects = {};
    document.querySelectorAll(".task-row[data-task-id]").forEach(el => {
        rects[el.dataset.taskId] = el.getBoundingClientRect().top;
    });
    pendingRects = rects;
}

export function initFlipObserver() {
    const list = document.querySelector(".task-list");
    if (!list) return;
    // Same per-element marker as initSortable, same reason — see the comment there.
    if (list.dataset.flipObserverBound === "true") return;
    list.dataset.flipObserverBound = "true";

    new MutationObserver(() => {
        if (!pendingRects) return;
        const oldRects = pendingRects;
        pendingRects = null;

        requestAnimationFrame(() => {
            document.querySelectorAll(".task-row[data-task-id]").forEach(el => {
                const oldTop = oldRects[el.dataset.taskId];
                if (oldTop === undefined) return;

                // Clear any leftover drag-preview transform *before* measuring — otherwise
                // this reads the transformed (preview) position instead of the row's true
                // post-patch layout position, throwing off the delta below.
                el.style.transition = "none";
                el.style.transform = "";
                const newTop = el.getBoundingClientRect().top;

                const delta = oldTop - newTop;
                if (Math.abs(delta) < 1) return;

                el.style.transform = `translateY(${delta}px)`;
                el.getBoundingClientRect(); // force reflow so the jump above applies before...

                requestAnimationFrame(() => {
                    el.style.transition = "transform 0.25s ease";
                    el.style.transform = "";
                });
            });
        });
    }).observe(list, { childList: true });
}
