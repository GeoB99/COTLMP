/*
 * PROJECT:     Cult of the Lamb Multiplayer Mod
 * LICENSE:     MIT (https://spdx.org/licenses/MIT)
 * PURPOSE:     Versioning class file
 * COPYRIGHT:	Copyright 2025 GeoB99 <geobman1999@gmail.com>
 */

/* IMPORTS ********************************************************************/

using COTLMP;

/* CLASSES & CODE *************************************************************/

/*
 * @brief
 * Contains static versioning data of the mod.
 * 
 * @class Version
 * Encapsulates the critical version data of the mod, such as the GUID, name
 * and version of the mod. The GUID is set to be unique and permanent, DO NOT CHANGE IT!
 */
namespace COTLMP.Data
{
    public class Version
	{
		public const string CotlMpGuid = "geob99.cotl.cotlmp";
		public const string CotlMpName = "Cult of the Lamb Multiplayer Mod Plug-In";
		public const string CotlMpVer = "0.0.0.1";
    }
}

/* EOF */
