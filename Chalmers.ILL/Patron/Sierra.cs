using System;
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
        private NpgsqlConnection connection; 

        public void Connect(string connectionString) {
            connection = new NpgsqlConnection(connectionString);
            connection.Open();
        }

        public void Disconnect()
        {
            connection.Close();
        }

        public SierraModel GetPatronInfoFromLibraryCardNumber(string barcode)
        {
            SierraModel model = new SierraModel();

            PopulateBasicPatronInfoFromLibraryCardNumber(barcode, model);

            PopulatePatronAddressInfoUsingExistingModel(model);

            return model;
        }

        public SierraModel GetPatronInfoFromLibraryCardNumberOrPersonnummer(string barcode, string pnr) 
        {
            SierraModel model = new SierraModel();
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
            }
            catch (Exception)
            {
                // NOP
            }

            if (!String.IsNullOrEmpty(model.id))
            {
                PopulatePatronAddressInfoUsingExistingModel(model);
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
                if (connection != null)
                {
                    try { connection.Close(); } catch (Exception) { }
                    connection.Dispose();
                    connection = null;
                }
            }
        }

        private void PopulateBasicPatronInfoFromLibraryCardNumber(string barcode, SierraModel model)
        {
            using (NpgsqlCommand command = new NpgsqlCommand("SELECT pv.id, pv.barcode, pv.ptype_code, vv.field_content as email, first_name, last_name, home_library_code, mblock_code from sierra_view.patron_record_fullname fn, sierra_view.patron_view pv, sierra_view.varfield_view vv where pv.id=fn.patron_record_id and pv.id=vv.record_id and vv.varfield_type_code='z' and lower(pv.barcode)=:barcode", connection))
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
                    }
                }
            }
        }

        private void PopulateBasicPatronInfoFromPersonnummer(string pnr, SierraModel model)
        {

            using (NpgsqlCommand command = new NpgsqlCommand("SELECT pv.id, pv.barcode, pv.ptype_code, vv.field_content as email, first_name, last_name, home_library_code, mblock_code from sierra_view.patron_record_fullname fn, sierra_view.patron_view pv, sierra_view.varfield_view vv where pv.id=fn.patron_record_id and pv.id=vv.record_id and vv.varfield_type_code='z' and pv.id=(select record_id from sierra_view.varfield_view vs where lower(vs.field_content)=:barcode)", connection))
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
                    }
                }
            }
        }

        private void PopulatePatronAddressInfoUsingExistingModel(SierraModel model)
        {
                using (NpgsqlCommand newcommand = new NpgsqlCommand("SELECT patron_record_address_type_id, addr1, addr2, addr3, village, city, region, postal_code, country from sierra_view.patron_record_address where patron_record_id=:record_id order by patron_record_address_type_id", connection))
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