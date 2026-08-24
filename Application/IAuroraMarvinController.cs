namespace AuroraMarvin
{
    using System.Collections.Generic;
    using System.Data;
    using Microsoft.Msagl.Drawing;

    public interface IAuroraMarvinController
    {
        void SetGame(int game);

        void GetGames();

        void GetRaces();

        void SetRace(int race);

        void SetDatabaseFile(string dbfile);

        void GetDesignsForHull(int hull);

        void GetShipDesign(string name);

        void SaveShipDesign(string hull, string name, string design);

        void UpdateNotes(string text);

        void UpdateGameNotes(string text);

        void SavePopulationResources();

        DataTable GetPopulationResources(int popId);

        bool CheckForChangedDatabase();

        Graph GetTechTree();

        void GetData();

        void SetMenuItemState(string name, bool @checked);

        DataTable GetMenuItemState();

        void SaveWealthPoints();
    }
}
