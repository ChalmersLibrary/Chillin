using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using System;

namespace Chalmers.ILL.Services
{
    public class FolioInstanceService : IFolioInstanceService
    {
        private readonly string path = "/instance-storage/instances";
        private readonly IFolioRepository _folioRepository;
        private readonly IJsonService _jsonService;

        public FolioInstanceService(IFolioRepository folioRepository, IJsonService jsonService)
        {
            _folioRepository = folioRepository;
            _jsonService = jsonService;
        }

        public Instance Post(InstanceBasic item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var response = _folioRepository.Post(path, _jsonService.SerializeObject(item));
            return _jsonService.DeserializeObject<Instance>(response);
        }
    }
}