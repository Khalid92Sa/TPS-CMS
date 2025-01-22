using CMS.Application.DTOs;
using CMS.Domain.Enums;
using CMS.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Web.Controllers;

public class TemplatesController : Controller
{
    private readonly ITemplatesService _templatesService;

    public TemplatesController(ITemplatesService templatesService) => _templatesService = templatesService;

    public async Task<ActionResult> Index()
    {
        var templates = await _templatesService.GetAllTemplatesAsync();
        return View(templates);
    }

    public async Task<ActionResult> Details(int id)
    {
        var template = await _templatesService.GetTemplateByIdAsync(id);
       
        if (template is null)
            return NotFound();
        return View(template);
    }

    public ActionResult Create()
    {
        var templatename = Enum.GetValues(typeof(TemplatesName))
       .Cast<TemplatesName>()
       .Select(name => new SelectListItem
       {
           Text = name.ToString(),
           Value = name.ToString()
       })
        .ToList();

        ViewBag.TemplateName = templatename;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(TemplatesDTO collection)
    {
        if (ModelState.IsValid)
        {
            await _templatesService.Create(collection);
            return RedirectToAction(nameof(Index));
        }


        var templatename = Enum.GetValues(typeof(TemplatesName))
       .Cast<TemplatesName>()
       .Select(name => new SelectListItem
       {
           Text = name.ToString(),
           Value = name.ToString()
       })
        .ToList();

        ViewBag.TemplateName = templatename;
        return View(collection);
    }

    public async Task<ActionResult> Edit(int id)
    {
        var template = await _templatesService.GetTemplateByIdAsync(id);
        if (template is null)
            return NotFound();
       
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(int id, TemplatesDTO collection)
    {
        if (id != collection.TemplatesId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            await _templatesService.Update(id, collection);
            return RedirectToAction(nameof(Index));
        }
        return View(collection);
    }


    public async Task<ActionResult> Delete(int id)
    {
        var template = await _templatesService.GetTemplateByIdAsync(id);

        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(int id, TemplatesDTO collection)
    {
        try
        {
            await _templatesService.Delete(id);
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }

}
