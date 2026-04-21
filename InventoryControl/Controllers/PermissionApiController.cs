namespace InventoryControl.Controllers;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/permission")]
public class PermissionApiController : ControllerBase
{
        private readonly IPermissionService _service;

        public PermissionApiController(IPermissionService service)
        {
            _service = service;
        }


        [HttpGet]
    [AuthorizePermissionHybrid("PERMISSION_GET")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAll();
            return Ok(data);
        }
   
    [HttpGet("modules")]
    [AuthorizePermissionHybrid("PERMISSION_GET")]
    public async Task<IActionResult> GetModules()
        {
            var data = await _service.GetModules();
            return Ok(data);
        }


        [HttpGet("{id}")]
    [AuthorizePermissionHybrid("PERMISSION_GET")]
    public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetById(id);
            return Ok(data);
        }


        [HttpPost]
    [AuthorizePermissionHybrid("PERMISSION_CREATE")]
    public async Task<IActionResult> Create([FromBody] RoleRequestDto dto)
        {
            var user = "system"; // ambil dari login nanti
            await _service.Create(dto, user);
            return Ok();
        }


        [HttpPut("{id}")]
    [AuthorizePermissionHybrid("PERMISSION_UPDATE")]

    public async Task<IActionResult> Update(string id, [FromBody] RoleRequestDto dto)
        {
            var user = "system";
            await _service.Update(id, dto, user);
            return Ok();
        }

    [HttpDelete("{id}")]
    [AuthorizePermissionHybrid("PERMISSION_DELETE")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.Delete(id);
        return Ok();
    }
}