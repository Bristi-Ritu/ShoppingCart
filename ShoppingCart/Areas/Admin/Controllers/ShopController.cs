using ShoppingCart.Areas.Admin.Models;
using ShoppingCart.Areas.Admin.Models.Data;
using ShoppingCart.Areas.Admin.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace ShoppingCart.Areas.Admin.Controllers
{
    public class ShopController : Controller
    {
        // GET: Admin/Shop/Categories
        public ActionResult Categories()
        {
            //Declare a list of models
            List<CategoryVM> categoryVMList;

            using (Db db = new Db())
            {
                //Init the list
                categoryVMList = db.Categories
                    .ToArray().OrderBy(x => x.Sorting)
                    .Select(x => new CategoryVM(x))
                    .ToList();

            }


            //Return view with list

            return View(categoryVMList);
        }


        // POST : Admin/Shop/AddNewCategory
        [HttpPost]
        public string AddNewCategory(string catName)
        {
            //Declare id
            string id;

            using (Db db = new Db())
            {

                //Check that the category name is unique
                if(db.Categories.Any(x => x.Name == catName))
                
                    return "titletaken";
                
                //Init DTO
                CategoryDTO dto = new CategoryDTO();
                //Add to DTO
                dto.Name = catName;
                dto.Slug = catName.Replace(" ", "-").ToLower();
                dto.Sorting = 100;
                //Save DTO
                db.Categories.Add(dto);
                db.SaveChanges();

                //Get the id
                id = dto.Id.ToString();

            }
            //Return id

            return id;





        }


        //POST : Admin/Shop/Reorder Categories/id
        [HttpPost]
        public void ReorderCategories(int[] id)
        {
            using (Db db = new Db())
            {
                //Set init count
                int count = 1;

                //Declare CategoryDTO
                CategoryDTO dto;
                //Set sorting for each category

                foreach (var catId in id)
                {
                    dto = db.Categories.Find(catId);
                    dto.Sorting = count;

                    db.SaveChanges();

                    count++;


                }

            }
        }

        //GET : Admin/Shop/Delete Category/id

        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {


                //Get the category
                CategoryDTO dto = db.Categories.Find(id);

                //Remove the category
                db.Categories.Remove(dto);
                //Save
                db.SaveChanges();

            }
            //Redirect


            return RedirectToAction("Categories");

        }

        //POST : Admin/Shop/Rename Category
        [HttpPost]
        public string RenameCategory(string newCatName , int id)
        {

            using (Db db = new Db())
            {

                //Check category name is unique
                if (db.Categories.Any( x => x.Name == newCatName))
                    return "titletaken";
                //Get DTO
                CategoryDTO dto = db.Categories.Find(id);
                //Edit DTO
                dto.Name = newCatName;
                dto.Slug = newCatName.Replace(" ", "-").ToLower();

                //Save
                db.SaveChanges();
            }
            //return
            return "ok";
        }


        //GET : Admin/Shop/Add Product
        [HttpGet]
        public ActionResult AddProduct()
        {
            //Init Model
            ProductVM model = new ProductVM();
            //Add select list of categories to model
            using(Db db = new Db())
            {
                model.Categories = new SelectList(db.Categories.ToList(),"Id","Name");
            }

            //Return view with model

            

            return View(model);
        }



        //POST : Admin/Shop/Add Product
        [HttpPost]
        public ActionResult AddProduct(ProductVM model , HttpPostedFileBase file)
        {
            //Check model state
            if (!ModelState.IsValid)
            {
                using(Db db = new Db())
                {

                    model.Categories = new SelectList(db.Categories.ToList() , "Id" ,"Name");

                    return View(model);
                }
                
            }

            //Make sure product name is unique

            using (Db db = new Db())
            {
                if(db.Products.Any( x => x.Name == model.Name))
                {

                    model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                    ModelState.AddModelError(" ", "That product name is taken!");
                    return View(model);

                    
                }

                
            }


            //Declare product id
            int id;
            //Init and save productDTO
            using(Db db = new Db())
            {

                ProductDTO product = new ProductDTO();

                product.Name = model.Name;
                product.Slug = model.Name.Replace(" ", "-").ToLower();
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;

                CategoryDTO catDTO = db.Categories.FirstOrDefault(x => x.Id == model.CategoryId);
                product.CategoryName = catDTO.Name;

                db.Products.Add(product);
                db.SaveChanges();

                //Get the id
                id = product.Id;
             
            }
            //Get inserted id
            TempData["SM"] = "You have added a product!";
            //Set TempData message

            #region Upload Image

            //Create necessary direction

            var orginalDirectory = new DirectoryInfo(string.Format("{0}Images\\Uploads",Server.MapPath(@"\")));

            
            var pathString1 = Path.Combine(orginalDirectory.ToString(), "Products");
            var pathString2 = Path.Combine(orginalDirectory.ToString(), "Products\\" + id.ToString() );
            var pathString3 = Path.Combine(orginalDirectory.ToString(), "Products\\" + id.ToString() + "\\Thumbs");
            var pathString4 = Path.Combine(orginalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery");
            var pathString5 = Path.Combine(orginalDirectory.ToString(), "Products\\" + id.ToString() + "\\Gallery\\Thumbs");


            if (!Directory.Exists(pathString1))
                Directory.CreateDirectory(pathString1);

            if (!Directory.Exists(pathString2))
                Directory.CreateDirectory(pathString2);

            if (!Directory.Exists(pathString3))
                Directory.CreateDirectory(pathString3);

            if (!Directory.Exists(pathString4))
                Directory.CreateDirectory(pathString4);

            if (!Directory.Exists(pathString5))
                Directory.CreateDirectory(pathString5);

            //Check a file if uploaded

            if (file != null && file.ContentLength > 0)
            {

                //Get file extention
                string ext = file.ContentType.ToLower();
                //verify extention

                if (ext != "image/jpg" && 
                    ext != "image/jpeg" &&
                    ext != "image/pjpeg" && 
                    ext != "image/gif" && 
                    ext != "image/x.png" &&
                    ext != "image/png")

                    {

                    using (Db db = new Db())
                    {
                       

                            model.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                            ModelState.AddModelError(" ", "The image was not uploaded-wrong image extention!");
                            return View(model);

                        

                    }


                }
                //Init Image Name

                string imageName = file.FileName;

                //Save image name to DTO 
                using(Db db = new Db())
                {
                    ProductDTO dto = db.Products.Find(id);
                    dto.ImageName = imageName;
                    db.SaveChanges();

                }

                //Set orginal and thumb image paths

                var path = string.Format("{0}\\{1}",pathString2 ,imageName );
                var path2 = string.Format("{0}\\{1}", pathString3, imageName);

                //Save orginal
                file.SaveAs(path);

                //create and save thumb

                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(path2);


            }

            #endregion

            //Redirect

            return RedirectToAction("AddProduct");
        }


    }
}