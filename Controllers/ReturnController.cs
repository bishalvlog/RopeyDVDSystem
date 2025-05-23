﻿using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RopeyDVDSystem.Data;
using RopeyDVDSystem.Models.ViewModels;
using System.Data;
using Dapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace RopeyDVDSystem.Controllers;

public class ReturnController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public ReturnController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public IEnumerable<ReturnModel> GetAllLoanRecords()
    {
        // The DVD copy that are loan list
        //IEnumerable<ReturnModel> loanRecord = from dt in _context.DVDTitles
        //                                      join dc in _context.DVDCopies on dt.DVDNumber equals dc.DVDNumber
        //                                      join l in _context.Loans on dc.CopyNumber equals l.CopyNumber
        //                                      join m in _context.Members on l.MemberNumber equals m.MemberNumber
        //                                      where l.DateReturn == DateTime.MinValue
        //                                      orderby l.DateOut descending, dt.DVDTitleName
        //                                      select new ReturnModel
        //                                      {
        //                                          CopyNumber = dc.CopyNumber,
        //                                          DVDTitleName = dt.DVDTitleName,
        //                                          DateOut = l.DateOut,
        //                                          DateDue = l.DateDue,
        //                                          MemberName = m.MemberFirstName + ' ' + m.MemberLastName,
        //                                          TotalLoan = (from la in _context.Loans
        //                                                       join dc in _context.DVDCopies on l.CopyNumber equals dc.CopyNumber
        //                                                       where la.DateOut == l.DateOut
        //                                                       select la.LoanNumber).Count(),
        //                                          LoanNumber = l.LoanNumber
        //                                      };

        //return loanRecord;
        string connectionString = _configuration.GetConnectionString("RopeyDB");

        //List<ReturnModel> returnlist = new List<ReturnModel>();

        using (var connection = new SqlConnection(connectionString))
        {

            connection.Open();
            var returnmodel = connection.Query<ReturnModel>("getreturnmodel", commandType: CommandType.StoredProcedure).ToList();

            return returnmodel;

        }

    }
    public IActionResult Index()
    {
        var loanRecord = GetAllLoanRecords();

        ViewBag.LoanedCopyNumberList =
            JsonSerializer.Serialize(_context.DVDCopies.Select(x => x.CopyNumber).Distinct().ToList());

        return View(loanRecord);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(string request)
    {
        string CopyNumber = Request.Form["SearchCopyNumber"];
        ViewBag.SearchCopyNumber = CopyNumber;

        // Get a list of all DVD Copy that are on loan
        ViewBag.LoanedCopyNumberList = JsonSerializer.Serialize(_context.DVDCopies.Select(x => x.CopyNumber).ToList());

        if (CopyNumber != null &&
            int.TryParse(CopyNumber, out var copyNumber) &&
            _context.DVDCopies.Where(x => x.CopyNumber == copyNumber).Count() > 0)
        {
            var loanRecord = from dt in _context.DVDTitles
                join dtc in _context.DVDCategories on dt.CategoryNumber equals dtc.CategoryNumber
                join dc in _context.DVDCopies on dt.DVDNumber equals dc.DVDNumber
                join l in _context.Loans on dc.CopyNumber equals l.CopyNumber
                join m in _context.Members on l.MemberNumber equals m.MemberNumber
                orderby l.DateOut descending
                where dc.CopyNumber == copyNumber
                select new ReturnModel
                {
                    CopyNumber = dc.CopyNumber,
                    DVDTitleName = dt.DVDTitleName,
                    DVDCategory = dtc.CategoryName,
                    DateOut = l.DateOut,
                    DateDue = l.DateDue,
                    DateReturn = l.DateReturn,
                    MemberName = m.MemberFirstName + ' ' + m.MemberLastName,
                    LoanNumber = l.LoanNumber,
                    Payment = l.ReturnAmount
                };

            if (loanRecord.Count() > 0)
            {
                var loanRecordFirst = loanRecord.First();
                if (loanRecordFirst.DateReturn != DateTime.MinValue) loanRecordFirst.OverDue = 1;
                ViewData["LoanRecord"] = loanRecordFirst;
            }

            return View();
        }

        if (CopyNumber == "")
        {
            // Get all the loan records
            var loanRecord = GetAllLoanRecords();
            return View(loanRecord);
        }

        return View();
    }


    public IActionResult Confirmation(int LoanID)
    {
        if (LoanID == 0 ||
            _context.Loans.Where(l => l.LoanNumber == LoanID).Count() == 0 ||
            _context.Loans.Where(l => l.LoanNumber == LoanID).First().DateReturn != DateTime.MinValue)
            return RedirectToAction("Index");

        // Get the loan record
        var currentLoan = (from dt in _context.DVDTitles
            join dtc in _context.DVDCategories on dt.CategoryNumber equals dtc.CategoryNumber
            join dc in _context.DVDCopies on dt.DVDNumber equals dc.DVDNumber
            join l in _context.Loans on dc.CopyNumber equals l.CopyNumber
            join m in _context.Members on l.MemberNumber equals m.MemberNumber
            where l.LoanNumber == LoanID
            select new ReturnModel
            {
                CopyNumber = dc.CopyNumber,
                DVDTitleName = dt.DVDTitleName,
                DVDCategory = dtc.CategoryName,
                DateOut = l.DateOut,
                DateDue = l.DateDue,
                MemberName = m.MemberFirstName + ' ' + m.MemberLastName,
                LoanNumber = l.LoanNumber,
                StandardCharge = l.ReturnAmount,
                PenaltyCharge = dt.PenaltyCharge
            }).First();

        var today = DateTime.Today;
        var overDueDays = (today - currentLoan.DateDue).TotalDays;
        if (overDueDays < 0) overDueDays = 0;

        currentLoan.OverDue = (int) overDueDays;
        currentLoan.Payment = currentLoan.StandardCharge + currentLoan.PenaltyCharge * (decimal) overDueDays;
        currentLoan.PenaltyCharge = currentLoan.PenaltyCharge * (decimal) overDueDays;

        ViewData["Return"] = currentLoan;
        return View();
    }

    [HttpPost]
    public IActionResult Confirmation(ReturnModel returnDVD)
    {
        if (returnDVD.LoanNumber == 0 ||
            returnDVD.CopyNumber == 0 ||
            _context.Loans.Where(l => l.LoanNumber == returnDVD.LoanNumber).Count() == 0 ||
            _context.DVDCopies.Where(dc => dc.CopyNumber == returnDVD.CopyNumber).Count() == 0)
            return RedirectToAction("Index");


        var loan = _context.Loans.Find(returnDVD.LoanNumber);
        loan.DateReturn = DateTime.Today;
        loan.ReturnAmount = returnDVD.Payment;
        _context.SaveChanges();


        var copy = _context.DVDCopies.Find(returnDVD.CopyNumber);
        copy.IsLoan = false;

        _context.SaveChanges();

        return RedirectToAction("Index");
    }
}