DayZ DB Access
==============

(WIP) Simple tool to show some data from a DayZ database. (Windows Only)

 - Supporting classic & Epoch DayZ databases.
 - Show on map:
	- players online (1)
	- players alive
	- vehicles
	- vehicle spawn points
	- deployables
 - Filter items by class
 - Filter items by time
 - Show players/vehicles/deployables inventory.
 - Add/Delete vehicle instances / spawnpoints
 - Bitmap selection for each World's type.
 - Map helper to do the link between selected bitmap & DB's coordinates
 - Vehicles Tab: associate an icon to each vehicle's class.
 - Deployables Tab: associate an icon to each deployable's class.
 - Scripts Tab:
	- backup database
	- remove destroyed vehicles (2)
	- spawn new vehicles (2)
	- remove old bodies (2)
	- remove old tents (2)
	- 3 custom SQL or BAT scripts can be set & called.

[1] : not available with Epoch's DB, due to lack of useable data.
[2] : disabled with Epoch's DB because useless.

Executable (ClickOnce) can be found here:

http://82.67.37.43/publish


Configuration
=============

 - Select the connection settings for your database.
 - Select the bitmap you want to use, try to use a big one. (no bitmap included)
 - Set icons for each type of vehicles and deployables.

Help
====

 - Click on icons on the map to see details in the Display Tab.
 - Clicking on an icon will select its type in the corresponding vehicle/deploayble's Tab.
 - Contextual menu on vehicle and spawn icons to remove them from the database.
 - Contextual menu on spawn icons to add a new one into the database.
 - Scripts Tab: 3 buttons can be set up to call custom SQL and BAT files.

If something goes wrong, all files created by this application are stored in your %appdata%\DayZDBAccess directory.

You can edit the config file, or delete all files to restore an empty configuration file.


History
=======
 - v2.8.3
	- some fixes for Epoch DB type
	- context menu on items will fill the property grid for selected item

 - v2.8.2
	- added Repair & Refuel command on vehicles

 - v2.8.1
	- refactored some display elements
	- added a trackbar to filter old items

 - v2.8.0
	- added command to add a vehicle's spawn point
	- refactored trails, handling player's death

 - v2.7.1
	- show data in percentage instead of floating point raw values

 - v2.7.0
	- refactored database connection
	- refactored global info
	- added vehicle parts in propertygrid

 - v2.6.0
	- added support for alphanumeric guids.
	- added context menu to reset list of vehicle & deployable types.
	- added destroyed icon for each vehicle types.
	- fixed some bugs.

 - v2.5.0
	- added checkboxes to filter displayed deployables/vehicles types.

 - v2.4.0
	- added button to backup the database.
	- added 3 buttons to call custom SQL files, or BAT files.

 - v2.3.0
	- added separate definition of Map helpers - Only Chernarus & Celle2 are done
	- redone the Map helper for Chernarus, now a grid + NWAF + Skalisty island

 - v2.2.0
	- added customizable deployables Tab.

 - v2.1.0
 	- new zooming system, removing the use of slow bicubic interpolation.
 	- fixed some user interactions when using Map helper

 - v2.0.0
 	- added support for Epoch database (not yet fully functional)
 	- added graphical helper to adapt DB coordinates to selected bitmap

 - v1.7.0
 	- changed for a fully custom display:
  		- mipmapped tiled display (DayZDB style)
  		- icons can be displayed off map.
  		- instant refresh of icons.
  		- faster display of big map.
  		- small memory usage.
  		- created when map selection is changed by user.
 	- spawn points info

 - v1.6.1
 	- modified tent display to add staches.

 - v1.6.0
 	- using new method to store config file, won't be lost when new update is published.
 	- changed handling of instances & worlds, now using world id from instance.
 	- bitmaps are set per world id, and reloaded when changed.

 - v1.5.0
 	- added customizable vehicle type per class, with corresponding icon.
 	- added contextual menu :
  		- delete vehicle
  		- delete spawnpoint (if no instanciated vehicles for this spawnpoint)

 - v1.4.0
 	- Using a dataset to manage maps data, allowing to define a map per instance id 

 - v1.3.5
 	- Added medical states, hunger & thirst percent in player's property grid
 	- Added cargo's total count in property grid

 - v1.3.0
 	- Refactored handling of icons, drastically faster and correctly displayed.
 	- Added trail on players & vehicles (refresh has to be fixed later)
 	- Refactored property grid, now displayed in order, expanded and auto refreshed.

 - v1.2.0
	- Refactored "Show Players Online"
 	- Refactored mySQL queries
 	- Refactored some code

 - v1.1.0
 	- selection of database and instance id.
 	- custom map display. (can be set up in config.xml, bitmaps not included)
 	- show on map
  		- alive players
  		- online players (will be refactored soon)
  		- valid and destroyed vehicles
  		- vehicle spawn points
  		- tents
 	- show data and inventory from selected player, vehicle or tent
 	- commands:
  		- Remove destroyed vehicles
  		- Spawn vehicles
  		- Remove bodies older than X days
  		- Remove tents not updated since X days

Icons used in this application have been borrowed from DayZDB and DayZST web sites, thanks to them even if I didn't asked permission for that... :S
