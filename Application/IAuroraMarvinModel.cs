namespace AuroraMarvin
{
    using System.Data;

    public interface IAuroraMarvinModel
    {
        int Game { get; set; }

        int Race { get; set; }

        string Dbfile { get; set; }

        DataTable GetGames();

        DataTable GetRaces();

        DataTable GetPopulations();

        void OpenAuroraDatabase();

        DataTable GetShips();

        DataTable GetDamagedShips();

        void OpenMarvinDatabase();

        DataTable GetLowCrewMorale();

        DataTable GetLowPopulationEfficieny();

        DataTable GetFreeConstructionFactories();

        DataTable GetObsoleteShips();

        DataTable GetCivMinesWithoutMassDriverDestination();

        DataTable GetUnusedTerraformers();

        bool CheckForChangedDatabase();

        DataTable GetShipsWithArmorDamage();

        DataTable GetWrecks();

        DataTable GetObsoleteTooledShipyards();

        DataTable GetShipDesignsForHull(int hull);

        DataTable GetMissingSectorCommanders();

        DataTable GetShipWithoutMsp();

        DataTable GetShipWithLowMsp();

        DataTable GetMissingAdminCommanders();

        DataTable GetHullDescriptions();

        DataTable GetShipDesign(string name);

        void SaveShipDesign(string hull, string name, string design);

        DataTable GetNotes(bool game);

        void UpdateNotes(bool game, string text);

        DataTable GetSelfSustainingColonistDestination();

        DataTable GetFullTrainedShipsinTrainingFleets();

        DataTable GetGameTime();

        DataTable GetWealthPoints();

        DataTable GetPopulationResources();

        DataTable GetLifePods();

        DataTable GetSavedResources();

        DataTable GetSavedPopulationResources(int popId);

        void SavePopulationResources(DataTable dt);

        void SaveWealthPoints(DataTable dt);

        DataTable GetPopulationsWithoutGovernor();

        DataTable GetTechData();

        DataTable GetUnusedMines();

        DataTable GetOpenFireFC();

        DataTable GetFreeOrdnanceFactories();

        void SetMenuItemState(string name, bool @checked);

        DataTable GetMenuItemState();

        DataTable GetTaxedCivMines();

        DataTable GetFreeFighterFactories();

        DataTable GetResearchFieldMismatch();

        DataTable GetResearchWithoutResearchFacility();

        DataTable GetSystemsWithUnsurveyedBodies();

        DataTable GetIdleSoriumHarvesters();

        DataTable GetPopulationsWithGroundSurveyPotential();

        DataTable GetIdleGeosurveyFormations();

        DataTable GetOrbitalMiningDiameter();

        DataTable GetIdleOrbitalMiners();

        DataTable GetDormantAncientConstructs();

        DataTable GetNotActiveAncientConstructs();

        DataTable GetHostileShipContacts();

        DataTable GetHostileGroundForceContacts();

        DataTable GetEventColours();

        DataTable GetAetherRiftGrowthRateReduction();

        DataTable GetFuelInTankers();

        DataTable GetSavedWealthPoints();

        DataTable GetIdleShipyards();

        DataTable GetIdleGroundForceConstructionComplex();
    }
}
