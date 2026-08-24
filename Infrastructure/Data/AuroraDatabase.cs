namespace AuroraMarvin
{
    using System;
    using System.Data;
    using System.Data.SQLite;

    internal class AuroraDatabase
    {
        private readonly string dbfile;
        private SQLiteConnection con;

        public AuroraDatabase(string dbfile)
        {
            this.dbfile = dbfile;
        }

        internal void Open()
        {
            string cs = @"URI=file:" + this.dbfile + ";Read Only=True";
            this.con = new SQLiteConnection(cs);
            this.con.Open();
        }

        internal DataTable GetDataTableFromQuery(string query)
        {
            var cmd = new SQLiteCommand(query, this.con);
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            DataTable dt = new DataTable();
            try
            {
                da.Fill(dt);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("SQL Exception: {0}.", ex.Message);
            }

            return dt;
        }

        internal void Close()
        {
            this.con.Close();
        }
    }
}