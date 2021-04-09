using Chalmers.ILL.Models;
using Chalmers.ILL.Repositories;
using System;

namespace Chalmers.ILL.Services
{
    public class FolioUserService : IFolioUserService
    {
        private readonly string path = "/users";
        private readonly IFolioRepository _folioRepository;
        private readonly IJsonService _jsonService;

        public FolioUserService
        (
            IFolioRepository folioRepository,
            IJsonService jsonService
        )
        {
            _folioRepository = folioRepository;
            _jsonService = jsonService;
        }

        public FolioUser ByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            var response = _folioRepository.ByQuery($"{path}?query=(username={userName})");
            var users = _jsonService.DeserializeObject<FolioUserNameQuery>(response);

            if (users.TotalRecords == 1)
            {
                return users.Users[0];
            }
            else if (users.TotalRecords > 1)
            {
                throw new FolioUserException($"Hittade flera användare med användarnamnet {userName}");
            }
            else
            {
                throw new FolioUserException($"Hittade ingen användare med användarnamnet {userName}");
            }
        }
    }
}