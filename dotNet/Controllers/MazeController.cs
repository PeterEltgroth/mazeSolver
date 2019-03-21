using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using dotNet.Models;
using dotNet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dotNet.Controllers {
  [Route ("solveMaze")]
  [ApiController]
  public class MazeController : ControllerBase {
    private IMazeService _mazeService;

    public MazeController(IMazeService mazeService) {
      this._mazeService = mazeService;
    }

    // POST solveMaze
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Post (IFormFile file) {
      var path = Path.GetTempFileName ();

      if (!Request.ContentType.Contains ("multipart/form-data")) {
        return BadRequest ("Request contains no file.");
      }

      if (0 == file.Length) {
        return BadRequest ("Maze file is empty.");
      }

      var mStream = new MemoryStream ();
      await file.CopyToAsync (mStream);
      byte[] b = mStream.ToArray ();
      string map = Encoding.ASCII.GetString (b);

      if (!_mazeService.validateMap (map)) {
        return BadRequest ("Maze contians invalid characters.");
      }

      Solution solution = _mazeService.solve (map);

      if (0 == solution.steps) {
        return Ok( new { info = "Maze has no solution!", original = solution.solution });
      }

      return Ok( new { steps = solution.steps, solution = solution.solution });
    }
  }
}