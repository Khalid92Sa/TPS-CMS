using CMS.Application.DTOs;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

public class CarrerOfferController : Controller
{
    private readonly ICarrerOfferService _carrerOfferService;
    private readonly IPositionService _positionService;


    public CarrerOfferController(ICarrerOfferService carrerOfferService, IPositionService positionService)
    {
        _carrerOfferService = carrerOfferService;
        _positionService = positionService;

    }

    public async Task<IActionResult> Index(CarrerOfferDTO crrerOfferDTO)
    {
        if (User.IsInRole("None"))
        {


            var result = await _carrerOfferService.GetAll();
            if (result.IsSuccess)
            {
                var positionsDTOs = result.Value;
                return View(positionsDTOs);
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View();
            }
        }
        else
        {
            if (User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

    }

    public async Task<IActionResult> Details(int id)
    {
        if (User.IsInRole("None"))
        {
            var result = await _carrerOfferService.GetById(id);

            var PositionsDTOs = await _positionService.GetAll();
            ViewBag.positionDTOs = new SelectList(PositionsDTOs.Value, "PositionId", "Name");

            if (result.IsSuccess)
            {
                var positionDTO = result.Value;
                return View(positionDTO);
            }


            else
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View();
            }
        }
        else
        {
            if (User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

    }

    public async Task<IActionResult> Create()
    {
        if (User.IsInRole("None"))
        {
            var PositionsDTOs = await _positionService.GetAll();
            ViewBag.positionDTOs = new SelectList(PositionsDTOs.Value, "PositionId", "Name");
            return View();
        }
        else
        {
            if (User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            };
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarrerOfferDTO carrerOfferDTO)
    {

        var positionDTOs = await _positionService.GetAll();
        ViewBag.positionDTOs = new SelectList(positionDTOs.Value, "PositionId", "Name");
        if (ModelState.IsValid)
        {
            var result = await _carrerOfferService.Insert(carrerOfferDTO);

            if (result.IsSuccess)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, result.Error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, "error validating the model");
        }

        return View(carrerOfferDTO);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (User.IsInRole("None"))
        {
            if (id <= 0)
            {
                return NotFound();
            }
            var result = await _carrerOfferService.GetById(id);
            var positionDTO = result.Value;
            if (positionDTO == null)
            {
                return NotFound();
            }
            var PositionsDTOs = await _positionService.GetAll();
            ViewBag.positionDTOs = new SelectList(PositionsDTOs.Value, "PositionId", "Name");
            return View(positionDTO);
        }
        else
        {
            if (User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CarrerOfferDTO carrerOfferDTO)
    {
        if (carrerOfferDTO == null)
        {
            ModelState.AddModelError(string.Empty, $"the career offer dto you are trying to update is null ");
            return RedirectToAction("Index");
        }

        var PositionsDTOs = await _positionService.GetAll();
        ViewBag.positionDTOs = new SelectList(PositionsDTOs.Value, "PositionId", "Name");
        if (ModelState.IsValid)
        {
            var result = await _carrerOfferService.Update(carrerOfferDTO);

            if (result.IsSuccess)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, result.Error);
            return View(carrerOfferDTO);
        }
        else
        {
            ModelState.AddModelError(string.Empty, $"the model state is not valid");
        }
        return View(carrerOfferDTO);
    }

    public async Task<IActionResult> Delete(int id)
    {
        if (User.IsInRole("None"))
        {
            var result = await _carrerOfferService.GetById(id);
            if (result.IsSuccess)
            {
                var positionDTO = result.Value;
                return View(positionDTO);
            }


            else
            {
                ModelState.AddModelError(string.Empty, result.Error);
                return View();
            }
        }
        else
        {
            if (User.Identity.IsAuthenticated)
            {
                return View("AccessDenied");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (id <= 0)
        {
            return BadRequest("invalid career offer id");
        }
        var result = await _carrerOfferService.Delete(id);
        if (result.IsSuccess)
        {
            return RedirectToAction("Index");
        }
        ModelState.AddModelError(string.Empty, result.Error);
        return View();
    }
}