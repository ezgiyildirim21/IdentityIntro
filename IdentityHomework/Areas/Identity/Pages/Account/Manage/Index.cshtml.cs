using IdentityHomework.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace IdentityHomework.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
            [Required(ErrorMessage = "This field cannot be empty")]
            [Display(Name = "New FirstName")]
            public string NewFirstName { get; set; }
            [Required(ErrorMessage = "This field cannot be empty")]
            [Display(Name = "New SurName")]
            public string NewLastName { get; set; }
            public string ImagePath { get; set; }

            public IFormFile Image { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var name = user.FirstName;
            var lastname = user.LastName;
            var imagePath = user.ImagePath;

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                NewFirstName = name,
                NewLastName = lastname,
                ImagePath = imagePath

            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }
            user.FirstName = Input.NewFirstName;
            user.LastName = Input.NewLastName;
            if (Input.Image != null)
            {
                user.ImagePath = AddNewImage();
            }
            await _userManager.UpdateAsync(user); //db yazmak için
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        private string AddNewImage()
        {
            string newImageName = null;
            if (Input.Image != null)
            {
                newImageName = Guid.NewGuid() + Path.GetExtension(Input.Image.FileName);
                string newDestinationPath = Path.Combine(_env.WebRootPath, "img", newImageName);
                using (FileStream stream = new FileStream(newDestinationPath, FileMode.Create))
                {
                    Input.Image.CopyTo(stream);
                }
            }
            return newImageName;
        }
    }
}
