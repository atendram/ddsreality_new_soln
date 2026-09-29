# Source review and implementation notes

Source: https://github.com/atendram/ddsreality_backend
Reviewed commit: 858584ce0ddcf794bde001adeb7b1492c55a9cf3

The repository contains static HTML, CSS, JavaScript, images, fonts and a legacy web.config. It does not contain the .csproj, Program.cs, controllers, views or models visible in the supplied Visual Studio screenshot. Therefore this delivery is a new standalone ASP.NET Core MVC solution using the existing website content/assets, not a verified patch to the unseen local project.

The live URL https://ddsrealty.in/ could not be retrieved in this environment. No live Lighthouse score, live load time, hosting capability or database connection has been claimed.

## Findings in the supplied source

- The homepage actively references eight CSS files and seven JavaScript libraries/files, including jQuery UI, Bootstrap, Owl Carousel and Magnific Popup-related scripts. The new pages replace these dependencies with one CSS and one small deferred JavaScript file per public/admin surface.
- Large repeated inline style blocks, negative spacing offsets and hotel-template remnants appear in the static pages. Some placeholder links use `javascript:;`, and a navigation call button uses `+919876543210`.
- The Ghanwat Accord map in source identifies the DDS office rather than a verified project location. It also contains contact details from another company. A recommended project slider contains “50 Vijay Nagar, Indore, INDIA”. These details need human review.
- Some project-card labels differ from their destination page name. All 16 project detail imports are drafts to allow review.
- Six existing blog posts were imported as drafts. Their dated claims and statements require editorial review.
- Reels are represented by images/links. The new footer stores a poster, title and target link per reel, without loading Instagram's embed runtime on every visit.

## What changed

- SQL Server content storage using `dds_ecommerce`, new isolated `[dds]` schema.
- Two site identities: Realty at `/`, Digital at `/dds`, sharing one admin at `/admin`.
- Cookie sign-in, built-in ASP.NET password hashing, five-failure account lockout, per-IP limits, secure cookies, server-side role protection, anti-forgery validation and session revocation on password change.
- Admin dashboard, content filtering/pagination, draft/publish actions, saved-content previews, upload validation, project media, site configuration, enquiry inbox, activity history and stale-edit conflict checks.
- Updated layout in dark blue-charcoal, cream and gold, matching the direction of the Realty logo. Site colours can be edited. A native system font stack avoids external font downloads.
- Mobile CSS layouts, reduced-motion support, subtle animation, form labels, semantic navigation and native accessible confirmation dialogs.
- Imported images converted/resized to WebP; hero image is eager/high-priority while supporting imagery and maps are lazy-loaded. Images and static files use cache headers; asset links are versioned.
- Public pages are server-rendered. EF queries use read-only tracking avoidance, filters, paging and a bounded number of homepage items. Database indexes support common lookups.
- Old published project `.html` URLs redirect to their new `/projects/{slug}` URLs. Deleted/draft project URLs return 404. `signup.html` has no public registration equivalent by design.

## Scope boundaries

DDS Digital has editable starter content because its own source/brand assets were not supplied. It is mounted at `/dds`; separate-domain routing is not configured. Enquiries are stored in the admin inbox; email/SMS/WhatsApp automation is not connected. Reels use click-to-open links, not uploaded video storage. Content is plain text, not arbitrary HTML. There is no ecommerce/cart/payment feature despite the existing database name. All administrators can manage both sites; per-site editor roles are not included.

## Technical references used

- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-8.0
- https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-8.0

The implementation uses PasswordHasher with a small CMS-specific admin store and cookie authentication, not the complete Identity UI or its public registration pages.
