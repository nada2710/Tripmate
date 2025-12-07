using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tripmate.Application.Services.Abstractions.Favorite;
using Tripmate.Application.Services.FavoriteList.DTOS;
using Tripmate.Application.Services.Hotels;
using Tripmate.Application.Services.Hotels.DTOS;
using Tripmate.Application.Services.Image;
using Tripmate.Domain.Common.Response;
using Tripmate.Domain.Entities.Models;
using Tripmate.Domain.Exceptions;
using Tripmate.Domain.Interfaces;
using Tripmate.Domain.Specification;
using Tripmate.Domain.Specification.FavoriteList;
using Tripmate.Domain.Specification.Hotels;

namespace Tripmate.Application.Services.FavoriteList
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(IMapper mapper, IUnitOfWork unitOfWork, ILogger<FavoriteService> logger)
        {
            _mapper=mapper;
            _unitOfWork=unitOfWork;
            _logger=logger;
        }
        public async Task<PaginationResponse<IEnumerable<FavoriteResponseDto>>> GetUserFavoritesAsync(string userId, FavoriteParameters parameters)
        {
            if (parameters.PageNumber <= 0)
                throw new BadRequestException("PageNumber must be greater than 0.");

            if (parameters.PageSize <= 0)
                throw new BadRequestException("PageSize must be greater than 0.");

            var dataSpec = new FavoriteSpecification(userId,parameters);
            var FavList = await _unitOfWork.Repository<Favorite, int>().GetAllWithSpecAsync(dataSpec);
            var totalCount = await _unitOfWork.Repository<Favorite, int>().CountAsync(new BaseSpecification<Favorite, int>(x => x.UserId == userId &&
                (!parameters.FavoriteType.HasValue || x.FavoriteType == parameters.FavoriteType)));

            if (FavList == null || !FavList.Any())
            {
                _logger.LogWarning("No favoriteItem found matching the provided criteria.");
                throw new NotFoundException("No favoriteItem found matching the provided criteria.");
            }
            var favsDto = _mapper.Map<IEnumerable<FavoriteResponseDto>>(FavList);

            return new PaginationResponse<IEnumerable<FavoriteResponseDto>>(
            favsDto,
            totalCount,
            parameters.PageNumber,
            parameters.PageSize
            );
        }
        public async Task<ApiResponse<FavoriteResponseDto>> AddFavoriteAsync(AddFavoriteDto addFavoriteDto)
        {
            if (addFavoriteDto is null)
            {
                _logger.LogError("Invalid Favorite data provided");
                throw new BadRequestException("Invalid Favorite data provided");
            }

            var spec = new FavoriteExistsSpecification(addFavoriteDto.UserId, addFavoriteDto.FavoriteType, addFavoriteDto.EntityId);
            var existing = await _unitOfWork.Repository<Favorite, int>().GetAllWithSpecAsync(spec);

            if (existing.Any())
            {
                _logger.LogWarning("Favorite already exists for user {UserId} and entity {EntityId}", addFavoriteDto.UserId, addFavoriteDto.EntityId);
                throw new BadRequestException("Favorite already exists for user.");
            }

            var favorite = _mapper.Map<Favorite>(addFavoriteDto);
            favorite.UserId = addFavoriteDto.UserId;
            favorite.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Favorite, int>().AddAsync(favorite);
            await _unitOfWork.SaveChangesAsync();

            var favoriteDto = _mapper.Map<FavoriteResponseDto>(favorite);

            return new ApiResponse<FavoriteResponseDto>(favoriteDto)
            {
                Success = true,
                StatusCode = 200,
                Message = "Favorite added successfully."
            };
        }

        public async Task<ApiResponse<bool>> DeleteFavoriteAsync(int favoriteId, string userId)
        {
            var favorite = await _unitOfWork.Repository<Favorite, int>().GetByIdAsync(favoriteId);

            if (favorite == null || favorite.UserId != userId)
            {
                _logger.LogWarning("Favorite with ID {FavoriteId} not found for user {UserId}", favoriteId, userId);
                throw new NotFoundException("Favorite not found for this user.");
            }

            _logger.LogInformation("Deleting favorite with ID {FavoriteId} for user {UserId}", favoriteId, userId);

            _unitOfWork.Repository<Favorite, int>().Delete(favorite);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted favorite with ID {FavoriteId} for user {UserId}", favoriteId, userId);

            return new ApiResponse<bool>(true)
            {
                Success = true,
                StatusCode = 200,
                Message = "Favorite deleted successfully."
            };
        }
    }
}
