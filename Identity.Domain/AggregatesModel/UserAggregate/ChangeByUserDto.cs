using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

<<<<<<< HEAD
namespace Identity.Domain.AggregatesModel.UserAggregate;

public class ChangeByUserDto
{
    public ChangeByUserDto(string? userName, string? userAddress, Uri? userImageCover)
    {
        UserName = userName;
        UserAddress = userAddress;
        UserImageCover = userImageCover;
    }

    public string? UserName { get; }

    public string? UserAddress { get; }

    public Uri? UserImageCover { get; }
=======
namespace Identity.Domain.AggregatesModel.UserAggregate
{
    public class ChangeByUserDto
    {
        public ChangeByUserDto(string? userName, string? userAddress, Uri? userImageCover)
        {
            UserName = userName;
            UserAddress = userAddress;
            UserImageCover = userImageCover;
        }

        public string? UserName { get; }

        public string? UserAddress { get; }

        public Uri? UserImageCover { get; }
    }
>>>>>>> 8e1a7f66420ec3bdbf7689044ea9f7d83b5d42f9
}
