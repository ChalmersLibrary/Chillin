using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Chalmers.ILL.Services
{
    public class FolioService : IFolioService
    {
        private readonly IFolioItemService _folioItemService;
        private readonly IFolioInstanceService _folioInstanceService;
        private readonly IFolioHoldingService _folioHoldingService;
        private readonly IFolioCirculationService _folioCirculationService;

        public FolioService
        (
            IFolioItemService folioItemService, 
            IFolioInstanceService folioInstanceService,
            IFolioHoldingService folioHoldingService,
            IFolioCirculationService folioCirculationService
        )
        {
            _folioItemService = folioItemService;
            _folioInstanceService = folioInstanceService;
            _folioHoldingService = folioHoldingService;
            _folioCirculationService = folioCirculationService;
        }

        public void SetItemToWithdrawn(string barcode)
        {
            var response = _folioItemService.ByQuery($"barcode={barcode}");
            if (response.TotalRecords > 0 && response.Items[0].Barcode == barcode)
            {
                response.Items[0].Status.Name = "Withdrawn";
                _folioItemService.Put(response.Items[0]);
            }
            //TODO - Slänga fel om det inte blir satt?
        }

        public void InitFolio(InstanceBasic instanceBasic, string barcode, string pickUpServicePoint, bool readOnlyAtLibrary, string folioUserId)
        {

            VerifyBarCode(barcode);
            var resInstance = CreateInstance(instanceBasic);
            var resHolding = CreateHolding(resInstance.Id);
            var resItem = CreateItem(resHolding.Id, barcode, readOnlyAtLibrary);
            var resCiruclation = CreateCirculation(resItem.Id, folioUserId, pickUpServicePoint);
        }

        private void VerifyBarCode(string barcode)
        {
            var response = _folioItemService.ByQuery($"barcode={barcode}");

            if (response.TotalRecords > 0)
            {
                throw new BarCodeException("Streckkoden finns redan i FOLIO");
            }
        }

        private Instance CreateInstance(InstanceBasic data)
        {
            //Skapa upp instanceBasic här i stället för i controllen.
            return _folioInstanceService.Post(data);
        }

        private Holding CreateHolding(string instanceId)
        {
            var data = new HoldingBasic
            {
                DiscoverySuppress = true,
                InstanceId = instanceId,
                PermanentLocationId = ConfigurationManager.AppSettings["holdingPermanentLocationId"].ToString(),
                CallNumber = "Interlibrary-in-loan",
                StatisticalCodeIds = new string[]
                {
                    ConfigurationManager.AppSettings["chillinStatisticalCodeId"].ToString()
                }
            };
            return _folioHoldingService.Post(data);
        }

        private Item CreateItem(string holdingId, string barCode, bool readOnlyAtLibrary)
        {
            var data = new ItemBasic
            {
                DiscoverySuppress = true,
                MaterialTypeId = ConfigurationManager.AppSettings["itemMaterialTypeId"].ToString(),
                PermanentLoanTypeId = readOnlyAtLibrary ? 
                    ConfigurationManager.AppSettings["itemPermanentLoanTypeIdInHouse"].ToString() : 
                    ConfigurationManager.AppSettings["itemPermanentLoanTypeId"].ToString(),
                HoldingsRecordId = holdingId,
                Barcode = barCode,
                Status = new Status { Name = "Available" },
                StatisticalCodeIds = new string[]
                {
                    ConfigurationManager.AppSettings["chillinStatisticalCodeId"].ToString()
                },
                CirculationNotes = new List<CirculationNotes>
                {
                    new CirculationNotes
                    {
                        NoteType = "Check in",
                        Note = "NÄR LÅNTAGARE ÅTERLÄMNAR: Lägg på hyllan för återlämnade fjärrlån på HB",
                        StaffOnly = true
                    }
                }
            };

            //if (readOnlyAtLibrary)
            //{
            //    data.CirculationNotes.Add(
            //        new CirculationNotes
            //        {
            //            NoteType = "Check out",
            //            Note = "Ej hemlån",
            //            StaffOnly = true
            //        });
            //}
            return _folioItemService.Post(data);
        }

        private Circulation CreateCirculation(string itemId, string requesterId, string pickupServicePoint)
        {
            var data = new CirculationBasic
            {
                ItemId = itemId,
                RequestDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                RequesterId = requesterId,
                RequestType = "Page",
                FulfilmentPreference = "Hold Shelf",
                PickupServicePointId = ServicePoints()[pickupServicePoint]
            };
            return _folioCirculationService.Post(data);
        }

        private Dictionary<string, string> ServicePoints() =>
            new Dictionary<string, string>()
            {
                { "Huvudbiblioteket", ConfigurationManager.AppSettings["servicePointHuvudbiblioteketId"].ToString() },
                { "Lindholmenbiblioteket", ConfigurationManager.AppSettings["servicePointLindholmenbiblioteketId"].ToString() },
                { "Arkitekturbiblioteket", ConfigurationManager.AppSettings["servicePointArkitekturbiblioteketId"].ToString() }
            };
    }
}