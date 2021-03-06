﻿namespace Dysphoria.Net.UrlRouting.Example.Controllers
{
	using System.Web.Mvc;
	using Dysphoria.Net.UrlRouting.Example.Models;
	using System;
	using System.Collections.Generic;

	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			using (var db = MonstersRepository.Get())
			{
				return View(new Tuple<IEnumerable<string>, IEnumerable<Monster>>(db.Categories, db.Monsters));
			}
		}

		public ActionResult About()
		{
			return View();
		}
	}
}
