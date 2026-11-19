/* A deliberately wide, finite table makes both axes overflow without inserting data into the task store.
   This teaching fixture renders every row; use ListView/TreeView virtualization for large real datasets. */
.demo-rows { width: 880px; }
.table-row {
    display: grid;
    grid-template-columns: 290px 70px 130px 1fr;
    gap: 12px;
    padding: 12px;
    border-bottom: 1px solid var(--nz-border);
}
.table-heading { font-weight: 600; background: var(--nz-surface); }
.muted { color: var(--nz-muted); }
.task-name { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
/* "Content fits" removes the wide columns as well as extra rows: neither axis should need a scrollbar. */
.compact { width: 100%; }
.compact .table-row { grid-template-columns: minmax(0, 1fr) 60px; }
