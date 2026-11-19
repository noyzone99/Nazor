:root {
    background: var(--surface);
    color: var(--text);
}

.registry {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 16px;
    height: 100%;
}

.toolbar {
    display: flex;
    align-items: center;
    gap: 8px;
}

.filter {
    width: 260px;

    @media (max-width: 720px) {
        width: 150px;
    }
}

.spacer { flex: 1; }

TextBox {
    background: var(--panel);
    color: var(--text);
    border-color: var(--border);

    &:focus { border-color: var(--accent); }
}

Button {
    background: var(--panel);
    color: var(--text);
    border-color: var(--border);
    transition: background 150ms ease-out;

    &:hover { background: var(--hover); }
}

.workspace {
    display: flex;
    flex-direction: row;
    gap: 16px;
    flex: 1;
    min-height: 0;

    @media (max-width: 720px) {
        flex-direction: column;
    }
}

.list {
    flex: 1;
    overflow-y: auto;
    padding: 8px;
    background: var(--surface-alt);
    border: 1px solid var(--border);
    border-radius: 6px;
}

.detail {
    display: flex;
    flex-direction: column;
    gap: 10px;
    width: 300px;
    padding: 12px;
    background: var(--panel);
    border: 1px solid var(--border);
    border-radius: 6px;

    @media (max-width: 720px) {
        width: auto;
    }
}

.title {
    font-size: 18px;
    font-weight: bold;
}

.hint { color: var(--muted); }

.code { color: var(--muted); }

.status { color: var(--muted); }
