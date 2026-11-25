# Lodgo — Environment filters and compact travel loader

This update is already applied to the project in this folder.

## Run
Run Start-Lodgo.ps1, then open http://localhost:5299/Account/Index.
A temporary verification instance was started at http://localhost:5399/Account/Index.

## Configure
Sign in as Admin or HotelOwner. Open the hotel dashboard and choose
"จัดการบรรยากาศ วิว และสระว่ายน้ำ", or visit /StayEnvironment/Edit.
Choose a hotel, enter its category, main view, pool availability, and distances in kilometres.
Save. Customers see the saved values on the catalog cards and hotel details.
Hotel owners can edit only assigned hotels. Admin can edit any catalog hotel.

Blank distances mean unknown; zero means adjacent to that area.
No inferred or invented hotel facts are seeded. Categories will have no matching results
until an authorized editor supplies the corresponding hotel data.

## Database
On startup, StayEnvironmentSchema.EnsureAsync creates stay_environments if missing.
Existing user, booking, ownership, and wallet data are preserved.
No manual migration or database reset is required.

## Navigation
Selecting a hotel shows a small centered travel-icon card while the page request runs, then hides it
when the new document is ready. Reduced motion is respected; Escape dismisses the
overlay and a dismiss button appears after eight seconds. Browser Back clears the overlay.
This is full-page navigation, not a progress percentage estimate.

## Verification
- dotnet build --no-restore -p:UseSharedCompilation=false -nodeReuse:false: passed.
- JavaScript syntax checks: passed.
- Local MySQL and HTTP checks: save/read, unknown values, invalid distance/category,
  anti-forgery enforcement, and customer access denial: passed.
- Browser: category filtering, empty results, reset, hotel navigation, and Back verified.
- Existing dependency warning NU1902 for MySqlConnector 2.4.0 remains unchanged.

## Applying the update archive
The archive contains only changed/new source files, with paths relative to the
Nontiwat_7362_L002 project directory. Back up that project, then extract here and
allow replacement of matching source files. It excludes local credentials, database
files, encryption keys, bin and obj.
