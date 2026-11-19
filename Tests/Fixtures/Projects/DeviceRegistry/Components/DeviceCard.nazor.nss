.device-card {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 12px;
    margin: 0 0 8px 0;
    background: var(--panel, #ffffff);
    color: var(--text, #1a1a1a);
    border: 1px solid var(--border, #d4d4d4);
    border-radius: 6px;
    transition: background 150ms ease-out, border-color 150ms ease-out;

    &.selected {
        background: var(--selected, #e5f0fb);
        border-color: var(--accent, #005fb8);
    }

    .heading {
        display: flex;
        align-items: center;
        gap: 8px;
    }

    .name {
        flex: 1;
        font-weight: bold;
    }

    .state {
        color: var(--muted, #5c5c5c);

        &.active { color: var(--positive, #0f7b0f); }
    }

    .select {
        align-self: flex-start;
        background: var(--accent, #005fb8);
        border-color: var(--accent, #005fb8);
        color: var(--on-accent, #ffffff);
        transition: background 200ms ease-out;

        &:hover { background: var(--accent-hover, #0067c0); }
    }
}
