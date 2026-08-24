namespace AuroraMarvin
{
    using System;
    using System.Data;
    using System.IO;

    public class AuroraMarvinModel : IDisposable, IAuroraMarvinModel
    {
        private AuroraDatabase ad;
        private MarvinDatabase dd;
        private DateTime lastRead = DateTime.MinValue;

        public AuroraMarvinModel()
        {
        }

        public int Game { get; set; }

        public int Race { get; set; }

        public string Dbfile { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public DataTable GetGames()
        {
            return this.ad.GetDataTableFromQuery("SELECT * FROM FCT_Game;");
        }

        public DataTable GetHullDescriptions()
        {
            return this.ad.GetDataTableFromQuery("SELECT * FROM FCT_HullDescription ORDER BY Description;");
        }

        public DataTable GetRaces()
        {
            return this.ad.GetDataTableFromQuery("SELECT * FROM FCT_Race WHERE NPR = 0 AND GameID = " + this.Game + ";");
        }

        public DataTable GetPopulations()
        {
            return this.ad.GetDataTableFromQuery("SELECT * FROM FCT_Population WHERE GameID = " + this.Game + " AND RaceID = " + this.Race + ";");
        }

        public DataTable GetShips()
        {
            return this.ad.GetDataTableFromQuery("SELECT * FROM FCT_Ship WHERE GameID = " + this.Game + " AND RaceID = " + this.Race + " AND ShippingLineID = 0;");
        }

        public DataTable GetDamagedShips()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, Name FROM FCT_Ship JOIN FCT_DamagedComponent ON FCT_DamagedComponent.ShipID = FCT_Ship.ShipID JOIN FCT_ShipDesignComponents ON FCT_DamagedComponent.ComponentID = FCT_ShipDesignComponents.SDComponentID WHERE FCT_Ship.GameID = " + this.Game + " AND RaceID = " + this.Race + " AND ShippingLineID = 0;");
        }

        public DataTable GetLowCrewMorale()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, CrewMorale FROM FCT_Ship WHERE FCT_Ship.GameID = " + this.Game + " AND RaceID = " + this.Race + " AND ShippingLineID = 0 And CrewMorale < 1.0;");
        }

        public DataTable GetFreeConstructionFactories()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, FCT_PopulationInstallations.Amount, SUM(COALESCE(Percentage, 0)) AS Percentage FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID LEFT JOIN FCT_IndustrialProjects ON FCT_IndustrialProjects.PopulationID = FCT_Population.PopulationID AND ProductionType IN (0,3,4) AND Queue = 0 JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID WHERE ConstructionValue > 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " AND FCT_PopulationInstallations.Amount > 0 GROUP BY PopName;");
        }

        public DataTable GetLowPopulationEfficieny()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, Efficiency FROM FCT_Population WHERE Efficiency < 1 AND Population > 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public DataTable GetObsoleteShips()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, ClassName FROM FCT_Ship JOIN FCT_ShipClass ON FCT_Ship.ShipClassID = FCT_ShipClass.ShipClassID WHERE Obsolete = 1 AND FCT_Ship.GameID = " + this.Game + " AND FCT_Ship.RaceID = " + this.Race + " AND ShippingLineID = 0;");
        }

        public DataTable GetUnusedTerraformers()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, Amount FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID WHERE TerraformingGasID = 0 AND DIM_PlanetaryInstallation.Name = 'Terraforming Installation' AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public DataTable GetShipsWithArmorDamage()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, SUM(Damage) FROM FCT_Ship JOIN FCT_ArmourDamage ON FCT_ArmourDamage.ShipID = FCT_Ship.ShipID WHERE ShippingLineID = 0 AND FCT_Ship.GameID = " + this.Game + " AND RaceID = " + this.Race + " GROUP BY ShipName;");
        }

        public DataTable GetWrecks()
        {
            return this.ad.GetDataTableFromQuery("SELECT ClassName, Name FROM FCT_Wrecks JOIN FCT_ShipClass ON FCT_Wrecks.ClassID = FCT_ShipClass.ShipClassID JOIN FCT_RaceSysSurvey ON FCT_Wrecks.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Wrecks.GameID = FCT_RaceSysSurvey.GameID AND FCT_Wrecks.RaceID = FCT_RaceSysSurvey.RaceID WHERE FCT_Wrecks.GameID = " + this.Game + " AND FCT_Wrecks.RaceID = " + this.Race + ";");
        }

        public DataTable GetObsoleteTooledShipyards()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipyardName, PopName, ClassName FROM FCT_Shipyard JOIN FCT_ShipClass ON FCT_Shipyard.BuildClassID = FCT_ShipClass.ShipClassID JOIN FCT_Population ON FCT_Shipyard.PopulationID = FCT_Population.PopulationID WHERE Obsolete = 1 AND FCT_Shipyard.RetoolClassID = 0 AND FCT_Shipyard.GameID = " + this.Game + " AND FCT_Shipyard.RaceID = " + this.Race + ";");
        }

        public DataTable GetMissingSectorCommanders()
        {
            return this.ad.GetDataTableFromQuery("SELECT SectorName FROM FCT_SectorCommand LEFT JOIN FCT_Commander ON FCT_SectorCommand.SectorCommandID = FCT_Commander.CommandID WHERE Name IS NULL AND FCT_SectorCommand.GameID = " + this.Game + " AND FCT_SectorCommand.RaceID = " + this.Race + ";");
        }

        public DataTable GetShipWithoutMsp()
        {
            return this.ad.GetDataTableFromQuery("SELECT CurrentMaintSupplies, FCT_Ship.ShipName, FCT_Fleet.FleetName FROM FCT_Ship JOIN FCT_ShipClass ON FCT_Ship.ShipClassID = FCT_ShipClass.ShipClassID JOIN FCT_Fleet ON FCT_Ship.FleetID = FCT_Fleet.FleetID WHERE CurrentMaintSupplies = 0 AND ShippingLineID = 0 AND NOT FCT_Ship.CurrentMaintSupplies = FCT_ShipClass.MaintSupplies AND FCT_Ship.GameID = " + this.Game + " AND FCT_Ship.RaceID = " + this.Race + " GROUP BY ShipName;");
        }

        public DataTable GetShipWithLowMsp()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_Ship.ShipName, FCT_Fleet.FleetName, ROUND((FCT_Ship.CurrentMaintSupplies / FCT_ShipClass.MaintSupplies * 100),1) AS SupplyLevel FROM FCT_Ship JOIN FCT_Fleet ON FCT_Ship.FleetID = FCT_Fleet.FleetID JOIN FCT_ShipClass ON FCT_Ship.ShipClassID = FCT_ShipClass.ShipClassID WHERE FCT_Ship.ShippingLineID = 0 AND SupplyLevel < 50 AND NOT FCT_Ship.CurrentMaintSupplies = FCT_ShipClass.MaintSupplies AND FCT_Ship.GameID = " + this.Game + " AND FCT_Ship.RaceID = " + this.Race + " ORDER BY SupplyLevel;");
        }

        public DataTable GetMissingAdminCommanders()
        {
            return this.ad.GetDataTableFromQuery("SELECT AdminCommandName FROM FCT_NavalAdminCommand LEFT JOIN FCT_Commander ON FCT_NavalAdminCommand.NavalAdminCommandID = FCT_Commander.CommandID WHERE Name IS NULL AND FCT_NavalAdminCommand.GameID = " + this.Game + " AND FCT_NavalAdminCommand.RaceID = " + this.Race + ";");
        }

        public DataTable GetCivMinesWithoutMassDriverDestination()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName FROM FCT_Population WHERE PurchaseCivilianMinerals = 1 AND MassDriverDest = 0 AND GameID = " + this.Game + " AND RaceID = " + this.Race + ";");
        }

        public DataTable GetSelfSustainingColonistDestination()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName FROM FCT_Population WHERE Population > 10 AND Capital = 0 AND ColonistDestination = 0 AND Efficiency = 1 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public void OpenAuroraDatabase()
        {
            this.ad = new AuroraDatabase(this.Dbfile);
            this.ad.Open();
        }

        public void OpenMarvinDatabase()
        {
            this.dd = new MarvinDatabase("AuroraMarvin.db");
            this.dd.Open();
        }

        public DataTable GetShipDesignsForHull(int hull)
        {
            return this.dd.GetDataTableFromQuery("SELECT Name, Design FROM ADT_ShipDesigns WHERE HullDescriptionID = " + hull + ";");
        }

        public DataTable GetShipDesign(string name)
        {
            return this.dd.GetDataTableFromQuery("SELECT Design FROM ADT_ShipDesigns WHERE Name = '" + name + "';");
        }

        public void SaveShipDesign(string hull, string name, string design)
        {
            this.dd.InsertShipDesign(hull, name, design);
        }

        public DataTable GetNotes(bool game)
        {
            if (game)
            {
                return this.dd.GetDataTableFromQuery("SELECT Note FROM ADT_Notes WHERE GameID = " + this.Game + " AND RaceID = " + this.Race + ";");
            }
            else
            {
                return this.dd.GetDataTableFromQuery("SELECT Note FROM ADT_Notes WHERE GameID = 0 AND RaceID = 0;");
            }
        }

        public void UpdateNotes(bool game, string text)
        {
            if (game)
            {
                this.dd.UpdateNotes(this.Game, this.Race, text);
            }
            else
            {
                this.dd.UpdateNotes(0, 0, text);
            }
        }

        public DataTable GetFullTrainedShipsinTrainingFleets()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, FleetName, AdminCommandName, TFPoints FROM FCT_Ship JOIN FCT_Fleet ON FCT_Ship.FleetID = FCT_Fleet.FleetID JOIN FCT_NavalAdminCommand ON FCT_Fleet.ParentCommandID = FCT_NavalAdminCommand.NavalAdminCommandID JOIN DIM_NavalAdminCommandType ON FCT_NavalAdminCommand.AdminCommandTypeID = DIM_NavalAdminCommandType.CommandTypeID WHERE DIM_NavalAdminCommandType.Description = 'Training' AND FCT_Ship.TFPoints = 500 AND FCT_Ship.GameID = " + this.Game + " AND FCT_Ship.RaceID = " + this.Race + " GROUP BY ShipName;");
        }

        public DataTable GetGameTime()
        {
            return this.ad.GetDataTableFromQuery("SELECT GameTime, StartYear FROM FCT_Game WHERE GameID = " + this.Game + ";");
        }

        public DataTable GetWealthPoints()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_Race.GameID, FCT_Race.RaceID, FCT_Game.GameTime, FCT_Race.WealthPoints FROM FCT_Race JOIN FCT_Game ON FCT_Race.GameID = FCT_Game.GameID WHERE FCT_Race.GameID = " + this.Game + ";");
        }

        public DataTable GetPopulationResources()
        {
            return this.ad.GetDataTableFromQuery(@"SELECT FCT_Population.GameID, FCT_Population.RaceID, PopulationID, GameTime, FuelStockpile, MaintenanceStockpile, Population, Duranium, Neutronium, Corbomite, Tritanium,
                Boronide, Mercassium, Vendarite, Sorium, Uridium, Corundium, Gallicite  FROM FCT_Population JOIN FCT_Game ON FCT_Population.GameID = FCT_Game.GameID
                JOIN FCT_Race ON FCT_Population.GameID = FCT_Race.GameID AND FCT_Population.RaceID = FCT_Race.RaceID
                WHERE FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public DataTable GetLifePods()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, Crew, Name FROM FCT_Lifepods JOIN FCT_RaceSysSurvey ON FCT_Lifepods.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Lifepods.GameID = FCT_RaceSysSurvey.GameID AND FCT_Lifepods.RaceID = FCT_RaceSysSurvey.RaceID WHERE FCT_Lifepods.GameID = " + this.Game + " AND FCT_Lifepods.RaceID = " + this.Race + ";");
        }

        public DataTable GetSavedResources()
        {
            return this.dd.GetDataTableFromQuery(@"SELECT GameTime, SUM(FuelStockpile), SUM(MaintenanceStockpile), SUM(Population), SUM(Duranium),
                SUM(Neutronium), SUM(Corbomite), SUM(Tritanium), SUM(Boronide), SUM(Mercassium), SUM(Vendarite), SUM(Sorium), SUM(Uridium),
                SUM(Corundium), SUM(Gallicite) FROM ADT_PopulationResources WHERE GameID = " + this.Game + " AND RaceID = " + this.Race + " GROUP BY GameTime;");
        }

        public DataTable GetSavedWealthPoints()
        {
            return this.dd.GetDataTableFromQuery("SELECT GameTime, WealthPoints FROM ADT_WealthPoints WHERE GameID = " + this.Game + " AND RaceID = " + this.Race + ";");
        }

        public DataTable GetSavedPopulationResources(int popId)
        {
            return this.dd.GetDataTableFromQuery(@"SELECT GameTime, Duranium, Neutronium, Corbomite, Tritanium, Boronide, Mercassium,
                Vendarite, Sorium, Uridium, Corundium, Gallicite FROM ADT_PopulationResources
                WHERE GameID = " + this.Game + " AND PopulationID = " + popId.ToString() + " AND RaceID = " + this.Race + ";");
        }

        public void SavePopulationResources(DataTable dt)
        {
            this.dd.UpdatePopulationResources(dt);
        }

        public void SaveWealthPoints(DataTable dt)
        {
            this.dd.UpdateWealthPoints(dt);
        }

        public bool CheckForChangedDatabase()
        {
            // The selected Aurora save can be deleted while MarvinS is watching it.
            // File.GetLastWriteTime() does not provide a valid database state in that
            // situation, so do not try to reload data from a file that no longer exists.
            if (string.IsNullOrEmpty(this.Dbfile) || !File.Exists(this.Dbfile))
            {
                return false;
            }

            DateTime lastWriteTime = File.GetLastWriteTime(this.Dbfile);
            if (lastWriteTime != this.lastRead)
            {
                this.lastRead = lastWriteTime;
                return true;
            }

            return false;
        }

        public DataTable GetPopulationsWithoutGovernor()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName FROM FCT_Population LEFT JOIN FCT_Commander ON FCT_Population.PopulationID = FCT_Commander.CommandID WHERE CommanderID IS NULL AND Population > 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public DataTable GetTechData()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_TechSystem.TechSystemID, FCT_TechSystem.Name, DIM_TechType.Description, DIM_ResearchField.FieldName, FCT_TechSystem.ConventionalSystem, FCT_TechSystem.StartingSystem, FCT_TechSystem.RuinOnly, FCT_TechSystem.Prerequisite1, FCT_TechSystem.Prerequisite2, FCT_TechSystem.AutomaticResearch, FCT_TechSystem.DevelopCost, CASE WHEN EXISTS (SELECT 1 FROM FCT_RaceTech rt WHERE rt.GameID = " + this.Game + " AND rt.RaceID = " + this.Race + " AND rt.TechID = FCT_TechSystem.TechSystemID) THEN 1 ELSE 0 END AS Researched, CASE WHEN EXISTS (SELECT 1 FROM FCT_ResearchProject rp WHERE rp.GameID = " + this.Game + " AND rp.RaceID = " + this.Race + " AND rp.TechID = FCT_TechSystem.TechSystemID) THEN 1 ELSE 0 END AS CurrentlyResearching FROM FCT_TechSystem JOIN DIM_TechType ON FCT_TechSystem.TechTypeID = DIM_TechType.TechTypeID JOIN DIM_ResearchField ON DIM_TechType.FieldID = DIM_ResearchField.ResearchFieldID WHERE FCT_TechSystem.RaceID = 0;");
        }

        public DataTable GetUnusedMines()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, FCT_PopulationInstallations.Amount, DIM_PlanetaryInstallation.Name FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID LEFT JOIN FCT_MineralDeposit ON FCT_Population.SystemBodyID = FCT_MineralDeposit.SystemBodyID WHERE DIM_PlanetaryInstallation.Name LIKE '%Mine%' AND MaterialID IS NULL AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public DataTable GetOpenFireFC()
        {
            return this.ad.GetDataTableFromQuery("SELECT ShipName, FleetName, FCNum FROM FCT_Ship JOIN FCT_Fleet ON FCT_Ship.FleetID = FCT_Fleet.FleetID JOIN FCT_FireControlAssignment ON FCT_Ship.ShipID = FCT_FireControlAssignment.ShipID WHERE OpenFire = 1 AND FCT_Ship.GameID = " + this.Game + " AND FCT_Ship.RaceID = " + this.Race + ";");
        }

        public DataTable GetFreeOrdnanceFactories()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, FCT_PopulationInstallations.Amount, SUM(COALESCE(Percentage, 0)) AS Percentage FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID LEFT JOIN FCT_IndustrialProjects ON FCT_IndustrialProjects.PopulationID = FCT_Population.PopulationID AND ProductionType = 1 AND Queue = 0 JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID WHERE OrdnanceProductionValue > 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " AND FCT_PopulationInstallations.Amount > 0 GROUP BY PopName;");
        }

        public DataTable GetFreeFighterFactories()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, FCT_PopulationInstallations.Amount, SUM(COALESCE(Percentage, 0)) AS Percentage FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID LEFT JOIN FCT_IndustrialProjects ON FCT_IndustrialProjects.PopulationID = FCT_Population.PopulationID AND ProductionType = 2 AND Queue = 0 JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID WHERE FighterProductionValue > 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " AND FCT_PopulationInstallations.Amount > 0 GROUP BY PopName;");
        }

        public DataTable GetTaxedCivMines()
        {
            return this.ad.GetDataTableFromQuery("SELECT DISTINCT PopName, FCT_RaceSysSurvey.Name FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID LEFT JOIN FCT_MineralDeposit ON FCT_Population.SystemBodyID = FCT_MineralDeposit.SystemBodyID JOIN FCT_RaceSysSurvey ON FCT_Population.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Population.GameID = FCT_RaceSysSurvey.GameID AND FCT_Population.RaceID = FCT_RaceSysSurvey.RaceID WHERE DIM_PlanetaryInstallation.Name = 'Civilian Mining Complex' AND PurchaseCivilianMinerals = 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + ";");
        }

        public void SetMenuItemState(string name, bool @checked)
        {
            this.dd.SetMenuItemState(name, @checked);
        }

        public DataTable GetMenuItemState()
        {
            return this.dd.GetDataTableFromQuery("SELECT * FROM ADT_MenuItemState;");
        }

        public DataTable GetResearchFieldMismatch()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_Commander.Name, FCT_Population.PopName, FCT_TechSystem.Name AS Research FROM FCT_ResearchProject JOIN FCT_Population ON FCT_Population.PopulationID = FCT_ResearchProject.PopulationID JOIN FCT_TechSystem ON FCT_ResearchProject.TechID = FCT_TechSystem.TechSystemID JOIN FCT_Commander ON FCT_Commander.CommanderType = 3 AND FCT_Commander.CommandType = 7 AND FCT_Commander.CommandID = FCT_ResearchProject.ProjectID WHERE FCT_ResearchProject.ResSpecID != FCT_Commander.ResSpecID AND FCT_ResearchProject.GameID = " + this.Game + " AND FCT_ResearchProject.RaceID = " + this.Race + ";");
        }

        public DataTable GetResearchWithoutResearchFacility()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_Population.PopName FROM FCT_ResearchProject JOIN FCT_Population ON FCT_ResearchProject.PopulationID = FCT_Population.PopulationID JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID LEFT JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID WHERE FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " GROUP BY FCT_Population.PopulationID HAVING SUM(ResearchValue) = 0");
        }

        public DataTable GetSystemsWithUnsurveyedBodies()
        {
            return this.ad.GetDataTableFromQuery(@"SELECT FCT_RaceSysSurvey.Name, SUM(CASE WHEN FCT_SystemBodySurveys.SystemBodyID IS NULL THEN 0 ELSE 1 END) AS SurveyedBodies, SUM(CASE WHEN FCT_SystemBody.SystemBodyID IS NULL THEN 0 ELSE 1 END) AS SystemBodies
                FROM FCT_RaceSysSurvey JOIN FCT_SystemBody ON FCT_RaceSysSurvey.SystemID = FCT_SystemBody.SystemID
                LEFT JOIN FCT_SystemBodySurveys ON FCT_SystemBody.SystemBodyID = FCT_SystemBodySurveys.SystemBodyID AND FCT_SystemBodySurveys.RaceID = FCT_RaceSysSurvey.RaceID
                LEFT JOIN FCT_AlienRace ON FCT_RaceSysSurvey.ControlRaceID = FCT_AlienRace.AlienRaceID AND FCT_AlienRace.ViewRaceID = " + this.Race + @"
                WHERE FCT_RaceSysSurvey.GameID = " + this.Game + @" and FCT_RaceSysSurvey.RaceID = " + this.Race + @" AND(FCT_AlienRace.ContactStatus != 0 OR FCT_AlienRace.ContactStatus IS NULL)
                AND FCT_RaceSysSurvey.SystemID NOT IN (
                SELECT FCT_Fleet.SystemID FROM FCT_Fleet
                LEFT JOIN FCT_Ship ON FCT_Fleet.FleetID = FCT_Ship.FleetID
                JOIN FCT_ShipClass ON FCT_Ship.ShipClassID = FCT_ShipClass.ShipClassID
                WHERE FCT_Fleet.GameID = " + this.Game + @" AND FCT_Fleet.RaceID = " + this.Race + @" AND FCT_ShipClass.GeoSurvey > 0
                )
                GROUP BY FCT_RaceSysSurvey.SystemID HAVING SUM(FCT_SystemBodySurveys.SystemBodyID) != SUM(FCT_SystemBody.SystemBodyID)");
        }

        public DataTable GetIdleSoriumHarvesters()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_Ship.ShipName, FCT_Fleet.FleetName, FCT_RaceSysSurvey.Name, FCT_Star.Component, FCT_SystemBody.PlanetNumber, FCT_SystemBody.Name AS BodyName FROM FCT_ShipClass LEFT JOIN FCT_Ship ON FCT_ShipClass.ShipClassID = FCT_Ship.ShipClassID JOIN FCT_Fleet ON FCT_Ship.FleetID = FCT_Fleet.FleetID JOIN FCT_SystemBody ON FCT_Fleet.OrbitBodyID = FCT_SystemBody.SystemBodyID JOIN FCT_RaceSysSurvey ON FCT_RaceSysSurvey.SystemID = FCT_SystemBody.SystemID AND FCT_RaceSysSurvey.RaceID = FCT_Fleet.RaceID JOIN FCT_Star ON FCT_SystemBody.StarID = FCT_Star.StarID LEFT JOIN FCT_MineralDeposit ON FCT_SystemBody.SystemBodyID = FCT_MineralDeposit.SystemBodyID AND FCT_RaceSysSurvey.SystemID = FCT_MineralDeposit.SystemID AND FCT_MineralDeposit.MaterialID = 8 WHERE FCT_ShipClass.Harvesters > 1 AND FCT_ShipClass.GameID = " + this.Game + " AND FCT_ShipClass.RaceID = " + this.Race + " AND FCT_Ship.ShippingLineID = 0 AND FCT_Fleet.OrbitBodyID != 0 AND Amount IS NULL");
        }

        public DataTable GetOrbitalMiningDiameter()
        {
            return this.ad.GetDataTableFromQuery("SELECT MAX(FCT_TechSystem.AdditionalInfo) AS MaxDiameter FROM FCT_TechSystem JOIN DIM_TechType ON FCT_TechSystem.TechTypeID = DIM_TechType.TechTypeID JOIN FCT_RaceTech ON FCT_TechSystem.TechSystemID = FCT_RaceTech.TechID WHERE DIM_TechType.Description = 'Maximum Orbital Mining Diameter' AND FCT_RaceTech.GameID = " + this.Game + " AND FCT_RaceTech.RaceID = " + this.Race);
        }

        public DataTable GetIdleOrbitalMiners()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_Ship.ShipName, FCT_Fleet.FleetName, FCT_RaceSysSurvey.Name, FCT_Star.Component, FCT_SystemBody.PlanetNumber, FCT_SystemBody.Name AS BodyName, FCT_SystemBody.Radius, SUM(CASE WHEN FCT_MineralDeposit.Amount IS NULL THEN 0 ELSE FCT_MineralDeposit.Amount END) AS Minerals FROM FCT_ShipClass LEFT JOIN FCT_Ship ON FCT_ShipClass.ShipClassID = FCT_Ship.ShipClassID JOIN FCT_Fleet ON FCT_Ship.FleetID = FCT_Fleet.FleetID JOIN FCT_SystemBody ON FCT_Fleet.OrbitBodyID = FCT_SystemBody.SystemBodyID JOIN FCT_RaceSysSurvey ON FCT_RaceSysSurvey.SystemID = FCT_SystemBody.SystemID AND FCT_RaceSysSurvey.RaceID = FCT_Fleet.RaceID JOIN FCT_Star ON FCT_SystemBody.StarID = FCT_Star.StarID LEFT JOIN FCT_MineralDeposit ON FCT_SystemBody.SystemBodyID = FCT_MineralDeposit.SystemBodyID WHERE FCT_ShipClass.MiningModules > 1 AND FCT_ShipClass.GameID = " + this.Game + " AND FCT_ShipClass.RaceID = " + this.Race + " AND FCT_Ship.ShippingLineID = 0 AND FCT_Fleet.OrbitBodyID != 0 GROUP BY FCT_Ship.ShipName, FCT_Fleet.FleetName, FCT_RaceSysSurvey.Name, FCT_Star.Component, FCT_SystemBody.PlanetNumber");
        }

        public DataTable GetPopulationsWithGroundSurveyPotential()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, FCT_RaceSysSurvey.Name, GroundMineralSurvey FROM FCT_Population JOIN FCT_SystemBody ON FCT_Population.SystemBodyID = FCT_SystemBody.SystemBodyID JOIN FCT_RaceSysSurvey ON FCT_SystemBody.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Population.RaceID = FCT_RaceSysSurvey.RaceID WHERE FCT_SystemBody.GroundMineralSurvey > 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " ORDER BY GroundMineralSurvey DESC");
        }

        public DataTable GetIdleGeosurveyFormations()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_GroundUnitFormation.Abbreviation, FCT_GroundUnitFormation.Name AS Formation, FCT_Population.PopName, FCT_RaceSysSurvey.Name FROM DIM_GroundComponentType LEFT JOIN FCT_GroundUnitClass ON DIM_GroundComponentType.ComponentTypeID = FCT_GroundUnitClass.ComponentA OR DIM_GroundComponentType.ComponentTypeID = FCT_GroundUnitClass.ComponentB OR DIM_GroundComponentType.ComponentTypeID = FCT_GroundUnitClass.ComponentC OR DIM_GroundComponentType.ComponentTypeID = FCT_GroundUnitClass.ComponentD JOIN FCT_GroundUnitFormationElement ON FCT_GroundUnitClass.GroundUnitClassID = FCT_GroundUnitFormationElement.ClassID JOIN FCT_GroundUnitFormation ON FCT_GroundUnitFormationElement.FormationID = FCT_GroundUnitFormation.FormationID JOIN FCT_Population ON FCT_GroundUnitFormation.PopulationID = FCT_Population.PopulationID JOIN FCT_SystemBody ON FCT_Population.SystemBodyID = FCT_SystemBody.SystemBodyID JOIN FCT_RaceSysSurvey ON FCT_SystemBody.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Population.RaceID = FCT_RaceSysSurvey.RaceID WHERE DIM_GroundComponentType.Geosurvey > 0 AND FCT_SystemBody.GroundMineralSurvey = 0 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " GROUP BY FCT_GroundUnitFormation.Name");
        }

        public DataTable GetDormantAncientConstructs()
        {
            return this.ad.GetDataTableFromQuery("SELECT FCT_RaceSysSurvey.Name FROM FCT_AncientConstruct JOIN FCT_SystemBodySurveys ON FCT_AncientConstruct.SystemBodyID = FCT_SystemBodySurveys.SystemBodyID JOIN FCT_SystemBody ON FCT_SystemBody.SystemBodyID = FCT_SystemBodySurveys.SystemBodyID AND FCT_SystemBody.GameID = FCT_SystemBodySurveys.GameID JOIN FCT_RaceSysSurvey ON FCT_RaceSysSurvey.SystemID = FCT_SystemBody.SystemID WHERE FCT_AncientConstruct.Active = 0 AND FCT_AncientConstruct.GameID = " + this.Game + " AND FCT_SystemBodySurveys.RaceID = " + this.Race + " AND FCT_RaceSysSurvey.RaceID = " + this.Race);
        }

        public DataTable GetNotActiveAncientConstructs()
        {
            return this.ad.GetDataTableFromQuery("SELECT PopName, Population, CASE WHEN Active IS 0 THEN 'Dormant' ELSE (SELECT FieldName FROM DIM_ResearchField WHERE DIM_ResearchField.ResearchFieldID = FCT_AncientConstruct.ResearchField) END AS FieldName, CASE WHEN Active IS 0 THEN 'Unknown' ELSE ((ResearchBonus - 1) *100) || '%' END AS Bonus FROM FCT_AncientConstruct JOIN FCT_SystemBodySurveys ON FCT_AncientConstruct.SystemBodyID = FCT_SystemBodySurveys.SystemBodyID JOIN FCT_Population ON FCT_SystemBodySurveys.SystemBodyID = FCT_Population.SystemBodyID WHERE FCT_AncientConstruct.GameID = " + this.Game + " AND FCT_Population.Population < 1 AND FCT_Population.RaceID = FCT_SystemBodySurveys.RaceID AND FCT_Population.RaceID = " + this.Race);
        }

        public DataTable GetHostileShipContacts()
        {
            return this.ad.GetDataTableFromQuery("SELECT Name, group_concat(ContactName) AS Contact, AlienRaceName, Abbrev, LastUpdate FROM FCT_Contacts JOIN FCT_RaceSysSurvey ON FCT_Contacts.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Contacts.DetectRaceID = FCT_RaceSysSurvey.RaceID JOIN FCT_AlienRace ON FCT_Contacts.DetectRaceID = FCT_AlienRace.ViewRaceID AND FCT_Contacts.ContactRaceID = FCT_AlienRace.AlienRaceID WHERE FCT_Contacts.ContactType = 1 AND FCT_AlienRace.ContactStatus = 0 AND FCT_Contacts.GameID = " + this.Game + " AND FCT_Contacts.DetectRaceID = " + this.Race + " GROUP BY ContactID ORDER BY LastUpdate");
        }

        public DataTable GetHostileGroundForceContacts()
        {
            return this.ad.GetDataTableFromQuery("SELECT Name, group_concat(ContactName) AS Contact, AlienRaceName, Abbrev, LastUpdate FROM FCT_Contacts JOIN FCT_RaceSysSurvey ON FCT_Contacts.SystemID = FCT_RaceSysSurvey.SystemID AND FCT_Contacts.DetectRaceID = FCT_RaceSysSurvey.RaceID JOIN FCT_AlienRace ON FCT_Contacts.DetectRaceID = FCT_AlienRace.ViewRaceID AND FCT_Contacts.ContactRaceID = FCT_AlienRace.AlienRaceID WHERE FCT_Contacts.ContactType = 12 AND FCT_AlienRace.ContactStatus = 0 AND FCT_Contacts.GameID = " + this.Game + " AND FCT_Contacts.DetectRaceID = " + this.Race + " GROUP BY ContactID ORDER BY LastUpdate");
        }

        public DataTable GetEventColours()
        {
            return this.ad.GetDataTableFromQuery("SELECT Description, AlertColour, TextColour FROM FCT_EventColour JOIN DIM_EventType ON FCT_EventColour.EventTypeID = DIM_EventType.EventTypeID WHERE FCT_EventColour.GameID = " + this.Game + " AND FCT_EventColour.RaceID = " + this.Race + " ORDER BY Description");
        }

        public DataTable GetAetherRiftGrowthRateReduction()
        {
            return this.ad.GetDataTableFromQuery("SELECT COALESCE(SUM((ResearchBonus - 1) * 10),0) AS RiftReduction FROM FCT_AncientConstruct JOIN FCT_Population ON FCT_AncientConstruct.SystemBodyID = FCT_Population.SystemBodyID WHERE FCT_Population.Population > 1 AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race);
        }

        public DataTable GetFuelInTankers()
        {
            return this.ad.GetDataTableFromQuery("SELECT COALESCE(SUM(FCT_Ship.Fuel),0) AS FuelInTankers FROM FCT_Ship JOIN FCT_ShipClass ON FCT_Ship.ShipClassID = FCT_ShipClass.ShipClassID WHERE FCT_ShipClass.FuelTanker = 1 AND FCT_Ship.ShippingLineID = 0 AND FCT_Ship.GameID = " + this.Game + " AND FCT_Ship.RaceID = " + this.Race + ";");
        }

        public DataTable GetIdleShipyards()
        {
            return this.ad.GetDataTableFromQuery("SELECT COUNT (FCT_ShipyardTask.ShipyardID) AS TaskCount, ShipyardName, PopName FROM FCT_Shipyard JOIN FCT_Population ON FCT_Shipyard.PopulationID = FCT_Population.PopulationID LEFT JOIN FCT_ShipyardTask ON FCT_Shipyard.ShipyardID = FCT_ShipyardTask.ShipyardID WHERE TaskType = 0 AND FCT_Shipyard.RaceID = " + this.Race + " AND FCT_Shipyard.GameID = " + this.Game + " GROUP BY FCT_Shipyard.ShipyardID HAVING TaskCount != Slipways;");
        }

        public DataTable GetIdleGroundForceConstructionComplex()
        {
            return this.ad.GetDataTableFromQuery("SELECT COUNT(FCT_GroundUnitTraining.PopulationID) AS QueueLength, PopName, Amount FROM FCT_Population JOIN FCT_PopulationInstallations ON FCT_Population.PopulationID = FCT_PopulationInstallations.PopID JOIN DIM_PlanetaryInstallation ON DIM_PlanetaryInstallation.PlanetaryInstallationID = FCT_PopulationInstallations.PlanetaryInstallationID LEFT JOIN FCT_GroundUnitTraining ON FCT_Population.PopulationID = FCT_GroundUnitTraining.PopulationID WHERE DIM_PlanetaryInstallation.Name = 'Ground Force Construction Complex' AND FCT_Population.GameID = " + this.Game + " AND FCT_Population.RaceID = " + this.Race + " GROUP BY PopName HAVING QueueLength < Amount");
        }

        protected virtual void Dispose(bool disposing)
        {
            this.ad.Close();
            this.dd.Close();
        }
    }
}