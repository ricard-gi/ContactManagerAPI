using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using contacts.Models;
using System.Security.Claims;

// Controllers/ContactsController.cs
using Microsoft.AspNetCore.Http;
using System.IO;



namespace contacts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requereix autenticació per a tots els mètodes
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Mètode auxiliar per obtenir l'ID de l'usuari loguejat
        private int GetLoggedUserId()
        {
            //User és  no és el model User de la base de dades, sinó una propietat de la classe 
            // base ControllerBase en ASP.NET Core que representa l'usuari autenticat que fa la petició.
            // aquí estem retornant el ID de l'usuari autenticat
            return int.Parse(User.FindFirst("UserId").Value);
        }

        // GET: api/Contacts
        // Només mostra els contactes de l'usuari loguejat
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contact>>> GetContacts()
        {
            var userId = GetLoggedUserId();
            return await _context.Contacts
                .Where(c => c.UserId == userId)  // Filtra els contactes de l'usuari loguejat
                .ToListAsync();
        }

        // GET: api/Contacts/5
        // Només accedeix als contactes de l'usuari loguejat
        [HttpGet("{id}")]
        public async Task<ActionResult<Contact>> GetContact(int id)
        {
            var userId = GetLoggedUserId();
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (contact == null)
            {
                return NotFound();
            }

            return contact;
        }

        // PUT: api/Contacts/5
        // Només permet modificar un contacte de l'usuari loguejat
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContact(int id, Contact contact)
        {
            var userId = GetLoggedUserId();

            // Comprova que el contacte pertany a l'usuari loguejat
            if (id != contact.Id || contact.UserId != userId)
            {
                return BadRequest("No tens permís per modificar aquest contacte.");
            }

            _context.Entry(contact).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContactExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Contacts
        // Crea un contacte associat a l'usuari loguejat
        [HttpPost]
        public async Task<ActionResult<Contact>> PostContact(Contact contact)
        {
            contact.UserId = GetLoggedUserId();  // Assigna l'usuari loguejat al contacte

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetContact", new { id = contact.Id }, contact);
        }

        // DELETE: api/Contacts/5
        // Només permet esborrar un contacte de l'usuari loguejat
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var userId = GetLoggedUserId();
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (contact == null)
            {
                return NotFound();
            }

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }

        [HttpPost("{id}/upload-photo")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound("Contact not found");
            }

            if (photo == null || photo.Length == 0)
            {
                return BadRequest("No file provided");
            }

            // Defineix el directori on es guardarà la foto
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(uploadsFolder); // Crea la carpeta si no existeix

            // Defineix el nom del fitxer
            var fileName = $"{Guid.NewGuid()}_{photo.FileName}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Guarda el fitxer al disc
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Desa el nom del fitxer a la base de dades
            contact.PhotoFileName = fileName;
            await _context.SaveChangesAsync();

            return Ok(new { fileName });
        }



    }
}
