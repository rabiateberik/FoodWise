using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class ShareRequest : BaseEntity
{
    public int ShareListingId { get; set; }

    public string RequesterUserId { get; set; } = null!;

    public decimal? MatchScore { get; set; }

    public ShareRequestStatus Status { get; set; } = ShareRequestStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.Now;

    public DateTime? RespondedAt { get; set; }

    public ShareListing ShareListing { get; set; } = null!;

    public Delivery? Delivery { get; set; }
}
