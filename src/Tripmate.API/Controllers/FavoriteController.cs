using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tripmate.Application.Services.Abstractions.Favorite;
using Tripmate.Application.Services.Abstractions.Hotel;
using Tripmate.Application.Services.FavoriteList.DTOS;
using Tripmate.Application.Services.Hotels;
using Tripmate.Application.Services.Hotels.DTOS;
using Tripmate.Domain.Specification.FavoriteList;
using Tripmate.Domain.Specification.Hotels;

namespace Tripmate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<FavoriteController> _logger;
        public FavoriteController(IFavoriteService favoriteService, ILogger<FavoriteController> logger)
        {
            _favoriteService=favoriteService;
            _logger=logger;
        }
        [HttpGet("GetUserFavorites")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetUserFavorites([FromQuery] string userId, [FromQuery] FavoriteParameters parameters)
        {
            _logger.LogInformation("Getting FavoriteList with parameters: PageNumber={PageNumber}, PageSize={PageSize}",
                parameters.PageNumber, parameters.PageSize);

            var Favorites = await _favoriteService.GetUserFavoritesAsync(userId,parameters);

            _logger.LogInformation("Successfully retrieved {Count} User Favorite", Favorites.Data?.Count() ?? 0);
            return Ok(Favorites);
        }
        [HttpPost("AddFavorite")]
        [AllowAnonymous]
        public async Task<IActionResult> AddFavorite([FromForm]AddFavoriteDto addFavoriteDto)
        {
            _logger.LogInformation("Adding new FavoriteItem");

            var result = await _favoriteService.AddFavoriteAsync(addFavoriteDto);

            _logger.LogInformation("Successfully added FavoriteItem");
            return Ok(result);
        }
        [HttpDelete("DeleteFavoriteItem")]
       
        public async Task<IActionResult> DeleteFavoriteItem(int favid, string userId)
        {
            _logger.LogInformation("Deleting favoriteItem with ID: {favItem}", favid);

            var result = await _favoriteService.DeleteFavoriteAsync(favid,userId);

            _logger.LogInformation("Successfully deleted favorite Item");
            return Ok(result);
        }

    }
}
