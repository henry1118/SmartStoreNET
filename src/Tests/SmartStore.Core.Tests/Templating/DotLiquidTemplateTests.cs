﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartStore.ComponentModel;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Orders;
using SmartStore.Templating;
using SmartStore.Templating.Liquid;

namespace SmartStore.Core.Tests.Templating
{
	[TestFixture]
	public class DotLiquidTemplateTests
	{
		private ITemplateEngine _engine;
		private IFormatProvider _deCulture;
		private IFormatProvider _enCulture;

		[SetUp]
		public virtual void SetUp()
		{
			_deCulture = CultureInfo.GetCultureInfo("de-DE");
			_enCulture = CultureInfo.GetCultureInfo("en-US");
			_engine = new DotLiquidTemplateEngine();
		}

		[Test]
		public void CanRenderAnonymousObject()
		{
			var now = DateTime.UtcNow;
			var data = new
			{
				FirstName = "John",
				LastName = "Doe",
				Audit = new { CreatedOn = DateTime.UtcNow }
			};
			var result = Render("Hello {{LastName}}, {{FirstName}}", data);
			Assert.AreEqual("Hello Doe, John", result);

			result = Render("{{FirstName}} {{LastName}} created on {{Audit.CreatedOn}}", data);
			Assert.AreEqual($"John Doe created on {now.ToString(_enCulture)}", result);

			result = Render("{{FirstName}} {{LastName}} created on {{Audit.CreatedOn}}", _deCulture, data);
			Assert.AreEqual($"John Doe created on {now.ToString(_deCulture)}", result);
		}

		[Test]
		public void CanRenderPoco()
		{
			var now = DateTime.UtcNow;
			var product = new Product
			{
				Name = "TV",
				Price = (decimal)9999.99,
				Discounts = new[] { "Sales", "Special" },
				Variants = new List<Variant> { new Variant { Name = "red" }, new Variant { Name = "blue" } }
			};

			var result = Render("{{Name}} has {{Discounts.size}} discounts, first name is '{{ Discounts[0] }}'. First Variant: {{ Variants.first.Name }}", product);
			Assert.AreEqual("TV has 2 discounts, first name is 'Sales'. First Variant: red", result);

			result = Render("{{product.Variants.size}} variants: {% for v in product.Variants %}{{ v.Name }}{% endfor %}", new { product = product });
			Assert.AreEqual("2 variants: redblue", result);
		}

		[Test]
		public void CanRenderHybridExpando()
		{
			var now = DateTime.UtcNow;
			var order = new Order
			{
				BillingAddress = new Address { FirstName = "John", LastName = "Doe" },
				Customer = new Customer { Email = "doe@john.com" },
				OrderTotal = (decimal)9999.99
			};

			var product1 = new SmartStore.Core.Domain.Catalog.Product { Name = "TV" };
			var product2 = new SmartStore.Core.Domain.Catalog.Product { Name = "Shoe" };
			order.OrderItems.Add(new OrderItem { Quantity = 2, Product = product1, PriceInclTax = 5000 });
			order.OrderItems.Add(new OrderItem { Quantity = 3, Product = product2, PriceInclTax = (decimal)4999.99 });

			var data = new HybridExpando();
			var orderExpando = new HybridExpando(order);
			data["Order"] = new HybridExpando(order);
			orderExpando["Custom"] = "Custom";

			var result = Render("{{ Order.OrderItems.size }} items, total: {{ Order.OrderTotal }} EUR", _deCulture, data);
			Assert.AreEqual("2 items, total: 9999,99 EUR", result);

			result = Render("{% for item in Order.OrderItems %}<div>{{ item.Quantity }}x{{ item.Product.Name | Upcase }}</div>{% endfor %}", _deCulture, data);
			Assert.AreEqual("<div>2xTV</div><div>3xSHOE</div>", result);

			result = Render("{% for item in Order.OrderItems %}{{ forloop.rindex }}{% unless forloop.last %}-{% endunless %}{% endfor %}", _deCulture, data);
			Assert.AreEqual("2-1", result);
		}

		private string Render(string template, object data)
		{
			return Render(template, _enCulture, data);
		}

		private string Render(string template, IFormatProvider formatProvider, object data)
		{
			var tpl = _engine.Compile(template);
			return tpl.Render(data, formatProvider);
		}
	}

	public class Product
	{
		public string Name { get; set; }
		public decimal Price { get; set; }
		public string[] Discounts { get; set; }
		public ICollection<Variant> Variants { get; set; }
	}

	public class Variant
	{
		public string Name { get; set; }
	}
}