﻿using Chalmers.ILL.Models;
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

        private Holding CreateHolding(string instanceId) => 
            _folioHoldingService.Post(new HoldingBasic(instanceId));

        private Item CreateItem(string holdingId, string barCode, bool readOnlyAtLibrary)
        {
            var item = new ItemBasic(barCode, holdingId, readOnlyAtLibrary);

            if (readOnlyAtLibrary)
            {
                item.CirculationNotes.Add(
                    new CirculationNote
                    {
                        NoteType = "Check out",
                        Note = "Ej hemlån",
                    });
            }

            return _folioItemService.Post(item);
        }

        private Circulation CreateCirculation(string itemId, string requesterId, string pickupServicePoint) => 
            _folioCirculationService.Post(new CirculationBasic(itemId, requesterId, ServicePoints()[pickupServicePoint]));

        private Dictionary<string, string> ServicePoints() =>
            new Dictionary<string, string>()
            {
                { "Huvudbiblioteket", ConfigurationManager.AppSettings["servicePointHuvudbiblioteketId"].ToString() },
                { "Lindholmenbiblioteket", ConfigurationManager.AppSettings["servicePointLindholmenbiblioteketId"].ToString() },
                { "Arkitekturbiblioteket", ConfigurationManager.AppSettings["servicePointArkitekturbiblioteketId"].ToString() }
            };
    }
}