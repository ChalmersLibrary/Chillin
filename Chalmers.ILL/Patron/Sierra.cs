using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Npgsql;
using Chalmers.ILL.Models;
using System.Configuration;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.UmbracoApi;
using Chalmers.ILL.Templates;

namespace Chalmers.ILL.Patron
{
    public class Sierra : IPatronDataProvider, IDisposable
    {
        private IUmbracoWrapper _umbraco;
        private ITemplateService _templateService;
        private NpgsqlConnection _connection;

        public Sierra(IUmbracoWrapper umbraco, ITemplateService templateService, string connectionString)
        {
            _umbraco = umbraco;
            _templateService = templateService;
            _connection = new NpgsqlConnection(connectionString);
            Connect();
        }

        public IPatronDataProvider Connect() {
            try
            {
                _connection.Open();
            }
            catch (Exception e)
            {
                _umbraco.LogError<Sierra>("Failed to open connection with Sierra.", e);
            }

            return this; // For call chaining
        }

        public IPatronDataProvider Disconnect()
        {
            try
            {
                _connection.Close();
            }
            catch (Exception e)
            {
                _umbraco.LogError<Sierra>("Failed to close connection with Sierra.", e);
            }

            return this; // For call chaining
        }

        public SierraModel GetPatronInfoFromLibraryCardNumber(string barcode)
        {
            SierraModel model = new SierraModel();

            var runCount = 0;
            var success = false;

            while (runCount < 2 && !success)
            {
                runCount++;

                try
                {
                    PopulateBasicPatronInfoFromLibraryCardNumber(barcode, model);
                    PopulatePatronAddressInfoUsingExistingModel(model);
                    success = true;
                }
                catch (Exception e)
                {
                    _umbraco.LogError<Sierra>("Failed to get patron info from library card number " + barcode + " from Sierra.", e);

                    // If we fail the first time we reconnect and try to fetch the information one more time.
                    if (runCount < 2)
                    {
                        Disconnect();
                        Connect();
                    }
                }
            }

            return model;
        }

        public SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr) 
        {
            SierraModel model = new SierraModel();

            var runCount = 0;
            var success = false;

            while (runCount < 2 && !success)
            {
                runCount++;

                try
                {
                    PopulateBasicPatronInfoFromPersonnummer(barcode, model);
                    if (String.IsNullOrEmpty(model.id))
                    {
                        if (barcode.Contains("-"))
                        {
                            PopulateBasicPatronInfoFromPersonnummer(barcode.Replace("-", ""), model);
                        }
                        else
                        {
                            string pnrdash = barcode.Substring(0, 6) + "-" + barcode.Substring(6, 4);
                            // Check if we have got a "personnummer" instead of a library card number?
                            PopulateBasicPatronInfoFromPersonnummer(pnrdash, model);
                        }
                    }

                    if (!String.IsNullOrEmpty(model.id))
                    {
                        PopulatePatronAddressInfoUsingExistingModel(model);
                    }

                    success = true;
                }
                catch (Exception e)
                {
                    _umbraco.LogError<Sierra>("Failed to get patron info from library card number or personnummer " + barcode + " from Sierra.", e);

                    // If we fail the first time we reconnect and try to fetch the information one more time.
                    if (runCount < 2)
                    {
                        Disconnect();
                        Connect();
                    }
                }
            }

            return model;
        }

        public SierraModel GetPatronInfoFromSierraId(string sierraId)
        {
            SierraModel model = new SierraModel();

            var runCount = 0;
            var success = false;

            while (runCount < 2 && !success)
            {
                runCount++;

                try
                {
                    PopulateBasicPatronInfoFromSierraId(sierraId, model);

                    success = true;
                }
                catch (Exception e)
                {
                    _umbraco.LogError<Sierra>("Failed to get patron info using sierra identifier " + sierraId + " from Sierra.", e);

                    // If we fail the first time we reconnect and try to fetch the information one more time.
                    if (runCount < 2)
                    {
                        Disconnect();
                        Connect();
                    }
                }
            }

            return model;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connection != null)
                {
                    try { _connection.Close(); } catch (Exception) { }
                    _connection.Dispose();
                    _connection = null;
                }
            }
        }

        private void PopulateBasicPatronInfoFromLibraryCardNumber(string barcode, SierraModel model)
        {
            if (_connection.State != ConnectionState.Open)
            {
                throw new SierraConnectionException("Status på koppling mot Sierra är inte \"open\", det är: \"" + _connection.State.ToString() + "\".");
            }

            using (NpgsqlCommand command = new NpgsqlCommand("SELECT pv.id, pv.barcode, pv.ptype_code, pv.record_num, vv.field_content as email, first_name, last_name, home_library_code, mblock_code from sierra_view.patron_record_fullname fn, sierra_view.patron_view pv, sierra_view.varfield_view vv where pv.id=fn.patron_record_id and pv.id=vv.record_id and vv.varfield_type_code='z' and lower(pv.barcode)=:barcode", _connection))
            {
                command.Parameters.Add(new NpgsqlParameter("barcode", NpgsqlTypes.NpgsqlDbType.Text));
                command.Parameters[0].Value = barcode.ToLower();

                using (NpgsqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.id = dr["id"].ToString();
                        model.barcode = dr["barcode"].ToString();
                        model.ptype = Convert.ToInt32(dr["ptype_code"]);
                        model.email = dr["email"].ToString();
                        model.first_name = dr["first_name"].ToString();
                        model.last_name = dr["last_name"].ToString();
                        model.home_library = dr["home_library_code"].ToString();
                        model.home_library_pretty_name = _templateService.GetPrettyLibraryNameFromLibraryAbbreviation(model.home_library);
                        model.mblock = dr["mblock_code"].ToString();
                        model.record_id = Convert.ToInt32(dr["record_num"].ToString());
                    }
                }
            }
        }

        private void PopulateBasicPatronInfoFromSierraId(string sierraId, SierraModel model)
        {
            if (_connection.State != ConnectionState.Open)
            {
                throw new SierraConnectionException("Status på koppling mot Sierra är inte \"open\", det är: \"" + _connection.State.ToString() + "\".");
            }

            using (NpgsqlCommand command = new NpgsqlCommand("SELECT pv.id, pv.barcode, pv.ptype_code, pv.record_num, vv.field_content as email, first_name, last_name, home_library_code, mblock_code from sierra_view.patron_record_fullname fn, sierra_view.patron_view pv, sierra_view.varfield_view vv where pv.id=fn.patron_record_id and pv.id=vv.record_id and vv.varfield_type_code='z' and pv.record_num=:sierraId", _connection))
            {
                command.Parameters.Add(new NpgsqlParameter("sierraId", NpgsqlTypes.NpgsqlDbType.Integer));
                command.Parameters[0].Value = Convert.ToInt32(sierraId);

                using (NpgsqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.id = dr["id"].ToString();
                        model.barcode = dr["barcode"].ToString();
                        model.ptype = Convert.ToInt32(dr["ptype_code"]);
                        model.email = dr["email"].ToString();
                        model.first_name = dr["first_name"].ToString();
                        model.last_name = dr["last_name"].ToString();
                        model.home_library = dr["home_library_code"].ToString();
                        model.home_library_pretty_name = _templateService.GetPrettyLibraryNameFromLibraryAbbreviation(model.home_library);
                        model.mblock = dr["mblock_code"].ToString();
                        model.record_id = Convert.ToInt32(dr["record_num"].ToString());
                    }
                }
            }
        }

        private void PopulateBasicPatronInfoFromPersonnummer(string pnr, SierraModel model)
        {
            if (_connection.State != ConnectionState.Open)
            {
                throw new SierraConnectionException("Status på koppling mot Sierra är inte \"open\", det är: \"" + _connection.State.ToString() + "\".");
            }

            using (NpgsqlCommand command = new NpgsqlCommand("SELECT pv.id, pv.barcode, pv.ptype_code, pv.record_num, vv.field_content as email, first_name, last_name, home_library_code, mblock_code from sierra_view.patron_record_fullname fn, sierra_view.patron_view pv, sierra_view.varfield_view vv where pv.id=fn.patron_record_id and pv.id=vv.record_id and vv.varfield_type_code='z' and pv.id=(select record_id from sierra_view.varfield_view vs where lower(vs.field_content)=:barcode)", _connection))
            {
                command.Parameters.Add(new NpgsqlParameter("barcode", NpgsqlTypes.NpgsqlDbType.Text));
                command.Parameters[0].Value = pnr.ToLower();

                using (NpgsqlDataReader dr = command.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        model.id = dr["id"].ToString();
                        model.barcode = dr["barcode"].ToString();
                        model.ptype = Convert.ToInt32(dr["ptype_code"]);
                        model.email = dr["email"].ToString();
                        model.first_name = dr["first_name"].ToString();
                        model.last_name = dr["last_name"].ToString();
                        model.home_library = dr["home_library_code"].ToString();
                        model.home_library_pretty_name = _templateService.GetPrettyLibraryNameFromLibraryAbbreviation(model.home_library);
                        model.mblock = dr["mblock_code"].ToString();
                        model.record_id = Convert.ToInt32(dr["record_num"].ToString());
                    }
                }
            }
        }

        private void PopulatePatronAddressInfoUsingExistingModel(SierraModel model)
        {
            if (_connection.State != ConnectionState.Open)
            {
                throw new SierraConnectionException("Status på koppling mot Sierra är inte \"open\", det är: \"" + _connection.State.ToString() + "\".");
            }

            using (NpgsqlCommand newcommand = new NpgsqlCommand("SELECT patron_record_address_type_id, addr1, addr2, addr3, village, city, region, postal_code, country from sierra_view.patron_record_address where patron_record_id=:record_id order by patron_record_address_type_id", _connection))
            {
                newcommand.Parameters.Add(new NpgsqlParameter("record_id", NpgsqlTypes.NpgsqlDbType.Bigint));
                newcommand.Parameters[0].Value = model.id;

                model.adress = new List<SierraAddressModel>();

                using (NpgsqlDataReader adr = newcommand.ExecuteReader())
                {
                    while (adr.Read())
                    {
                        model.adress.Add(new SierraAddressModel()
                        {
                            addresscount = adr["patron_record_address_type_id"].ToString(),
                            addr1 = adr["addr1"].ToString(),
                            addr2 = adr["addr2"].ToString(),
                            addr3 = adr["addr3"].ToString(),
                            village = adr["village"].ToString(),
                            city = adr["city"].ToString(),
                            region = adr["region"].ToString(),
                            postal_code = adr["postal_code"].ToString(),
                            country = adr["country"].ToString()
                        });
                    }
                }
            }
        }
    }
}