namespace AuroraMarvin
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    public interface IAuroraMarvinView
    {
        void SetController(IAuroraMarvinController cont);

        void SetGames(DataTable games);

        void SetRaces(DataTable races);

        void SetPopulations(DataTable populations);

        void SetShips(DataTable ships);

        void AddOverviewMessage(string key, List<string> msg);

        void SetHullDescriptions(DataTable hullDescriptions);

        void SetDesignsForHull(DataTable shipDesigns);

        void SetShipDesign(DataTable design);

        void SetNotes(DataTable notes);

        void SetGameNotes(DataTable notes);

        void SetGameTime(DateTime gameTime);

        void SetResources(DataTable ressources);

        void SetEventColours(DataTable events);

        void ClearOverviewMessages();

        void RefreshOverviewMessage();

        void SetFuelInTankers(DataTable fuel);

        void SetGameStartTime(int startyear);

        void SetWealthPoints(DataTable dataTable);
    }
}
