using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using System;

namespace Chalmers.ILL.Services
{
    public class FolioCirculationService : IFolioCirculationService
    {
        private readonly string path = "/circulation/requests";
        private readonly IFolioRepository _folioRepository;
        private readonly IJsonService _jsonService;

        public FolioCirculationService(IFolioRepository folioRepository, IJsonService jsonService)
        {
            _folioRepository = folioRepository;
            _jsonService = jsonService;
        }

        public Circulation Post(CirculationBasic item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var response = _folioRepository.Post(path, _jsonService.SerializeObject(item));
            return _jsonService.DeserializeObject<Circulation>(response);
        }
    }
}