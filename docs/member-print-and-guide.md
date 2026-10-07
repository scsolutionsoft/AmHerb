# Member dashboard and A5 shipping labels

- `/Member`: six quick actions and accessible Bootstrap step guides for opening a store, buying, POS, shipping, Token and network. Guides can be reopened and navigate to the relevant live pages. They do not mark business tasks as completed.
- `/MyStore/Orders`: each online order links to its A5 address preview. `/MyStore/ShippingLabel/{id}` requires the owning member's store; another member receives 404. Responses are not cached.
- Sender defaults to store name/phone and the member profile address. Sender edits apply only to the current preview; update `/Member#profile` for a permanent address change. Recipient uses the order's address snapshot.
- Print setup: A5 portrait, actual size 100%, disable browser headers/footers. Browser print supports Save as PDF. This is an address label, not purchased carrier postage. Pending-payment orders show a warning.
- The print button checks for a sender name/address and content exceeding one A5 page. Native browser printing remains available. Preview scales down on small screens; print keeps physical A5 dimensions.
- No database schema changes or new migrations are needed for these additions.

Validation: the opt-in MemberStoreBrowserTests cover ownership isolation, preview data, sender edits/validation, A5 PDF dimensions and page count, mobile overflow, and all six guide forward/back controls. Generated PDF and screenshots are in `artifacts/screenshots`.

Postal label refinement: separate sender postcode field (prefilled from the trailing five digits of the profile address), five-digit postcode validation for both parties, recipient postcode on its own line, and a reserved top-right area for postal service labels. The design follows clear sender/recipient addressing guidance in Thailand Post's packing guide, https://www.thailandpost.co.th/PR/youtube_vdo/pdf/how_to_packing.pdf . A5 is the requested print size, not a claim of carrier-issued postage certification.
