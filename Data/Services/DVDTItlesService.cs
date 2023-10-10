using eTickets.Data.Base;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RopeyDVDSystem.Data.ViewModels;
using RopeyDVDSystem.Models;
using System.Data;

namespace RopeyDVDSystem.Data.Services;

public class DVDTitlesService : EntityBaseRepository<DVDTitle>, IDVDTitlesService
{
    private readonly ApplicationDbContext _context;
    private IConfiguration _configuration;
    string connectionstring;

    

    public DVDTitlesService(ApplicationDbContext context, IConfiguration configuration) : base(context)
    {
        _context = context;
        _configuration = configuration;
        connectionstring = configuration.GetConnectionString("RopeyDB");
    }

    public async Task AddNewDVDTitleAsync(newDVDTitleVM data)
    {
        using (SqlConnection connection =new SqlConnection (connectionstring))


        {
            using (SqlCommand command = new SqlCommand ("adddvdnew",connection))
            {
                connection.Open();
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@DvdNumber", data.DVDNumber);
                command.Parameters.AddWithValue("@DVDTitleName", data.DVDTitleName);
                command.Parameters.AddWithValue("@CategoryNumber", data.CategoryNumber);
                command.Parameters.AddWithValue("@StudioNumber", data.StudioNumber);
                command.Parameters.AddWithValue("@ProducerNumber", data.ProducerNumber);
                command.Parameters.AddWithValue("@DVDPictureURL", data.DVDPictureURL);
                command.Parameters.AddWithValue("@DateReleased", data.DateReleased);
                command.Parameters.AddWithValue("@StandardCharge", data.StandardCharge);
                command.Parameters.AddWithValue("@PenaltyCharge", data.PenaltyCharge);
                command.Parameters.AddWithValue("@StatementType","Insert");
                //command.Parameters.AddWithValue("")
                connection.Close();
               
                command.ExecuteNonQueryAsync();  

            }
        }
            /*var newDVD = new DVDTitle
            {
                DVDTitleName = data.DVDTitleName,
                CategoryNumber = data.CategoryNumber,
                StudioNumber = data.StudioNumber,
                ProducerNumber = data.ProducerNumber,
                DVDPictureURL = data.DVDPictureURL,
                DateReleased = data.DateReleased,
                StandardCharge = data.StandardCharge,
                PenaltyCharge = data.PenaltyCharge
            };
            await _context.DVDTitles.AddAsync(newDVD);
            await _context.SaveChangesAsync();*/

        //    /* ADD Movie Actors*/
        //    foreach (var actorId in data.ActorNumbers)
        //    {
        //        var newCastMember = new CastMember
        //        {
        //            DVDNumber = newDVD.DVDNumber,
        //            ActorNumber = actorId
        //        };
        //        await _context.CastMembers.AddAsync(newCastMember);
        //    }

        //await _context.SaveChangesAsync();
    }

    public async Task<DVDTitle> GetDVDTitleByIdAsync(int id)
    {
        var dVDDetails = await _context.DVDTitles
            .Include(s => s.Studio)
            .Include(p => p.Producer)
            .Include(c => c.DVDCategory)
            .Include(cm => cm.CastMembers).ThenInclude(a => a.Actor)
            .FirstOrDefaultAsync(n => n.DVDNumber == id);

        return dVDDetails;
    }

    public async Task<NewDVDTitleDropdownsVM> GetNewDVDTitleDropdownsValues()
    {
        var response = new NewDVDTitleDropdownsVM
        {
            Actors = await _context.Actors.OrderBy(n => n.ActorFirstName).ToListAsync(),
            DVDCategories = await _context.DVDCategories.OrderBy(n => n.CategoryName).ToListAsync(),
            Studios = await _context.Studios.OrderBy(n => n.StudioName).ToListAsync(),
            Producers = await _context.Producers.OrderBy(n => n.ProducerName).ToListAsync()
        };


        return response;
    }

    public async Task UpdateDVDTitleAsync(newDVDTitleVM data)
    {
        var dbDVDTitle = await _context.DVDTitles.FirstOrDefaultAsync(n => n.DVDNumber == data.DVDNumber);

        if (dbDVDTitle != null)
        {
            dbDVDTitle.DVDTitleName = data.DVDTitleName;
            dbDVDTitle.CategoryNumber = data.CategoryNumber;
            dbDVDTitle.StudioNumber = data.StudioNumber;
            dbDVDTitle.ProducerNumber = data.ProducerNumber;
            dbDVDTitle.DVDPictureURL = data.DVDPictureURL;
            dbDVDTitle.DateReleased = data.DateReleased;
            dbDVDTitle.StandardCharge = data.StandardCharge;
            dbDVDTitle.PenaltyCharge = data.PenaltyCharge;
            await _context.SaveChangesAsync();
        }

        //Remove existing actors
        var existingActorsDb = _context.CastMembers.Where(n => n.DVDNumber == data.DVDNumber).ToList();
        _context.CastMembers.RemoveRange(existingActorsDb);
        await _context.SaveChangesAsync();


        //ADD Movie Actors
        foreach (var actorId in data.ActorNumbers)
        {
            var newCastMember = new CastMember
            {
                DVDNumber = data.DVDNumber,
                ActorNumber = actorId
            };
            await _context.CastMembers.AddAsync(newCastMember);
        }

        await _context.SaveChangesAsync();
    }
}