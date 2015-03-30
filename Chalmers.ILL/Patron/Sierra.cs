﻿using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Npgsql;
using Chalmers.ILL.Models;
using System.Configuration;
using Chalmers.ILL.Utilities;

namespace Chalmers.ILL.Patron
{
    public class Sierra : IPatronDataProvider, IDisposable
    {
        private NpgsqlConnection _connection;

        public Sierra(string connectionString)
        {
            _connection = new NpgsqlConnection(connectionString);
            Connect();
        }

        public IPatronDataProvider Connect() {
            try
            {
                _connection.Open();
            }
            catch (Exception)
            {
                // NOP, we will retry this automatically on next usage.
            }

            return this; // For call chaining
        }

        public IPatronDataProvider Disconnect()
        {
            try
            {
                _connection.Close();
            }
            catch (Exception)
            {
                // NOP, we will retry this automatically on next usage.
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
                catch (Exception)
                {
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
                catch (Exception)
                {
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
                        model.mblock = dr["mblock_code"].ToString();
                        model.record_id = Convert.ToInt32(dr["record_num"].ToString());
                    }
                }
            }
        }

        private void PopulateBasicPatronInfoFromPersonnummer(string pnr, SierraModel model)
        {
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
                        model.mblock = dr["mblock_code"].ToString();
                        model.record_id = Convert.ToInt32(dr["record_num"].ToString());
                    }
                }
            }
        }

        private void PopulatePatronAddressInfoUsingExistingModel(SierraModel model)
        {
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