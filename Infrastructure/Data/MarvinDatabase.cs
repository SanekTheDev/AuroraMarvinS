namespace AuroraMarvin
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.IO;

    internal class MarvinDatabase
    {
        private readonly string dbFile;
        private SQLiteConnection con;

        public MarvinDatabase(string dbFile)
        {
            this.dbFile = dbFile;
        }

        internal void Open()
        {
            bool dBinitNeeded = false;

            if (!File.Exists(this.dbFile))
            {
                dBinitNeeded = true;
            }

            string cs = @"URI=file:" + this.dbFile + ";";
            this.con = new SQLiteConnection(cs);
            this.con.Open();

            List<string> sql = new List<string>();
            List<string> sqlver1 = new List<string>
            {
                "CREATE TABLE \"ADT_MenuItemState\" (\"Name\" TEXT, \"State\" INTEGER, PRIMARY KEY(\"Name\"))",
                "CREATE TABLE \"ADT_DBVersion\" (\"Version\" INTEGER)",
                "INSERT INTO \"ADT_DBVersion\" (\"Version\") VALUES (\"1\")",
            };
            List<string> sqlver2 = new List<string>
            {
                "CREATE TABLE \"ADT_Notes2\" (\"GameID\" INTEGER, \"RaceID\" INTEGER, \"Note\" TEXT, PRIMARY KEY(\"GameID\",\"RaceID\"))",
                "INSERT INTO \"ADT_Notes2\" (\"GameID\", \"RaceID\", \"Note\") VALUES (0, 0, (SELECT note FROM \"ADT_Notes\" LIMIT 1))",
                "DROP TABLE \"ADT_Notes\"",
                "ALTER TABLE \"ADT_Notes2\" RENAME TO \"ADT_Notes\"",
                "UPDATE \"ADT_DBVersion\" SET \"Version\" = 2",
            };
            List<string> sqlver3 = new List<string>
            {
                "CREATE TABLE \"ADT_PopulationResources\" (\"GameID\" INTEGER, \"PopulationID\" INTEGER, \"GameTime\" DOUBLE, \"FuelStockpile\" REAL, \"MaintenanceStockpile\" REAL, \"Population\" REAL, \"Duranium\" REAL, \"Neutronium\" REAL, \"Corbomite\" REAL, \"Tritanium\" REAL, \"Boronide\" REAL, \"Mercassium\" REAL, \"Vendarite\" REAL, \"Sorium\" REAL, \"Uridium\" REAL, \"Corundium\" REAL, \"Gallicite\" REAL);",
                "UPDATE \"ADT_DBVersion\" SET \"Version\" = 3",
            };
            List<string> sqlver4 = new List<string>
            {
                "ALTER TABLE \"ADT_PopulationResources\" ADD \"RaceID\" INTEGER",
                "DELETE FROM \"ADT_PopulationResources\"",
                "DROP TABLE \"ADT_Resources\";",
                "CREATE TABLE \"ADT_WealthPoints\" (\"GameID\" INTEGER, \"RaceID\" INTEGER, \"GameTime\" DOUBLE, \"WealthPoints\" REAL);",
                "UPDATE \"ADT_DBVersion\" SET \"Version\" = 4",
            };

            List<string> sqlver5 = new List<string>
            {
                "DELETE FROM ADT_PopulationResources WHERE rowid in (SELECT rowid FROM (SELECT rowid, row_number() over (partition by GameID, PopulationID, GameTime, RaceID) as n FROM ADT_PopulationResources) WHERE n > 1);",
                "CREATE UNIQUE INDEX ADT_PopulationResourcesIndex ON ADT_PopulationResources(\"GameID\", \"PopulationID\", \"GameTime\", \"RaceID\");",
                "UPDATE \"ADT_DBVersion\" SET \"Version\" = 5",
            };

            if (dBinitNeeded)
            {
                Console.WriteLine("Initialize DB");

                sql = new List<string>
                {
                    "CREATE TABLE \"ADT_Notes\"(\"note\"  TEXT)",
                    "CREATE TABLE \"ADT_Resources\"(\"GameID\" TEXT, \"Resources\" TEXT, PRIMARY KEY(\"GameID\"))",
                    "CREATE TABLE \"ADT_ShipDesigns\"(\"HullDescriptionID\" INTEGER, \"Name\" TEXT, \"Design\" TEXT)",
                    "INSERT INTO \"ADT_Notes\" (\"note\") VALUES (\"\")",
                };
                sql.AddRange(sqlver1);
                sql.AddRange(sqlver2);
                sql.AddRange(sqlver3);
                sql.AddRange(sqlver4);
                sql.AddRange(sqlver5);
            }
            else
            {
                DataTable dbver = this.GetDataTableFromQuery("SELECT name FROM sqlite_master WHERE type='table' AND name='ADT_DBVersion';");
                if (dbver.Rows.Count == 0)
                {
                    sql.AddRange(sqlver1);
                }
                else
                {
                    DataTable ver = this.GetDataTableFromQuery("SELECT Version FROM ADT_DBVersion;");
                    switch (int.Parse(ver.Rows[0]["Version"].ToString()))
                    {
                        case 1:
                            sql.AddRange(sqlver2);
                            sql.AddRange(sqlver3);
                            sql.AddRange(sqlver4);
                            sql.AddRange(sqlver5);
                            break;
                        case 2:
                            sql.AddRange(sqlver3);
                            sql.AddRange(sqlver4);
                            sql.AddRange(sqlver5);
                            break;
                        case 3:
                            sql.AddRange(sqlver4);
                            sql.AddRange(sqlver5);
                            break;
                        case 4:
                            sql.AddRange(sqlver5);
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach (string s in sql)
            {
                SQLiteCommand command = new SQLiteCommand(s, this.con);
                command.ExecuteNonQuery();
            }
        }

        internal void Close()
        {
            this.con.Close();
        }

        internal DataTable GetDataTableFromQuery(string query)
        {
            var cmd = new SQLiteCommand(query, this.con);
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            return dt;
        }

        internal void InsertShipDesign(string hull, string name, string design)
        {
            SQLiteCommand command = new SQLiteCommand("INSERT INTO ADT_ShipDesigns (HullDescriptionID, Name, Design) VALUES (@hull, @name, @design)", this.con);
            command.Parameters.AddWithValue("@hull", hull);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@design", design);
            command.ExecuteNonQuery();
        }

        internal void UpdateNotes(int game, int race, string text)
        {
            SQLiteCommand command = new SQLiteCommand("REPLACE INTO ADT_Notes(GameID, RaceID, Note) VALUES(@game, @race, @text)", this.con);
            command.Parameters.AddWithValue("@game", game);
            command.Parameters.AddWithValue("@race", race);
            command.Parameters.AddWithValue("@text", text);
            command.ExecuteNonQuery();
        }

        internal void UpdatePopulationResources(DataTable dt)
        {
            using (var command = new SQLiteCommand(this.con))
            using (var transaction = this.con.BeginTransaction())
            {
                foreach (DataRow r in dt.Rows)
                {
                    command.CommandText = @"INSERT INTO ADT_PopulationResources (GameID, RaceID, PopulationID, GameTime, FuelStockpile,
                        MaintenanceStockpile, Population, Duranium, Neutronium, Corbomite, Tritanium, Boronide, Mercassium,
                        Vendarite, Sorium, Uridium, Corundium, Gallicite) VALUES (@gameid, @raceid, @populationid, @gametime, @fuelstockpile,
                        @maintenancestockpile, @population, @duranium, @neutronium, @corbomite, @tritanium, @boronide, @mercassium,
                        @vendarite, @sorium, @uridium, @corundium, @gallicite)";
                    command.Parameters.AddWithValue("@gameid", r["GameID"]);
                    command.Parameters.AddWithValue("@raceid", r["RaceID"]);
                    command.Parameters.AddWithValue("@populationid", r["PopulationID"]);
                    command.Parameters.AddWithValue("@gametime", r["GameTime"]);
                    command.Parameters.AddWithValue("@fuelstockpile", r["FuelStockpile"]);
                    command.Parameters.AddWithValue("@maintenancestockpile", r["MaintenanceStockpile"]);
                    command.Parameters.AddWithValue("@population", r["Population"]);
                    command.Parameters.AddWithValue("@duranium", r["Duranium"]);
                    command.Parameters.AddWithValue("@neutronium", r["Neutronium"]);
                    command.Parameters.AddWithValue("@corbomite", r["Corbomite"]);
                    command.Parameters.AddWithValue("@tritanium", r["Tritanium"]);
                    command.Parameters.AddWithValue("@boronide", r["Boronide"]);
                    command.Parameters.AddWithValue("@mercassium", r["Mercassium"]);
                    command.Parameters.AddWithValue("@vendarite", r["Vendarite"]);
                    command.Parameters.AddWithValue("@sorium", r["Sorium"]);
                    command.Parameters.AddWithValue("@uridium", r["Uridium"]);
                    command.Parameters.AddWithValue("@corundium", r["Corundium"]);
                    command.Parameters.AddWithValue("@gallicite", r["Gallicite"]);
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (SQLiteException ex)
                    {
                        Console.WriteLine("SQL Exception: {0}.", ex.Message);
                    }
                }

                transaction.Commit();
            }
        }

        internal void UpdateWealthPoints(DataTable dt)
        {
            using (var command = new SQLiteCommand(this.con))
            using (var transaction = this.con.BeginTransaction())
            {
                foreach (DataRow r in dt.Rows)
                {
                    command.CommandText = @"INSERT INTO ADT_WealthPoints (GameID, RaceID, GameTime, WealthPoints)
                        VALUES (@gameid,  @raceid, @gametime, @wealthpoints)";
                    command.Parameters.AddWithValue("@gameid", r["GameID"]);
                    command.Parameters.AddWithValue("@raceid", r["RaceID"]);
                    command.Parameters.AddWithValue("@gametime", r["GameTime"]);
                    command.Parameters.AddWithValue("@wealthpoints", r["WealthPoints"]);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        internal void SetMenuItemState(string name, bool @checked)
        {
            SQLiteCommand command = new SQLiteCommand("REPLACE INTO ADT_MenuItemState(Name, State) VALUES(@name, @checked)", this.con);
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@checked", @checked);
            command.ExecuteNonQuery();
        }
    }
}