/* Structural layout belongs to the reusable list; scroll appearance/policy inherits from App's Window.
   min-height: 0 lets this flex child shrink so overflowing rows scroll instead of pushing out the footer.
   Keeping policy out of the generic list lets another caller supply different NSS defaults.
   The class targets only the viewport, not the keyed row Containers declared by this same component. */
.item-list {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
}
