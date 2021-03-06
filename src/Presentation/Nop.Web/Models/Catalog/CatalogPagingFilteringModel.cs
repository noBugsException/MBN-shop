﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.UI.Paging;
using Nop.Web.Infrastructure.Cache;
using Nop.Services.Directory;

namespace Nop.Web.Models.Catalog
{
	/// <summary>
	/// Filtering and paging model for catalog
	/// </summary>
	public partial class CatalogPagingFilteringModel : BasePageableModel
	{
		#region Ctor

		/// <summary>
		/// Constructor
		/// </summary>
		public CatalogPagingFilteringModel()
		{
			this.AvailableSortOptions = new List<SelectListItem>();
			this.AvailableViewModes = new List<SelectListItem>();
			this.PageSizeOptions = new List<SelectListItem>();
			this.PriceRangeFilter = new PriceRangeFilterModel();
			this.SpecificationFilter = new SpecificationFilterModel();
			this.ManufacturerFilter = new ManufacturerFilterModel();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Price range filter model
		/// </summary>
		public PriceRangeFilterModel PriceRangeFilter { get; set; }
		/// <summary>
		/// Specification filter model
		/// </summary>
		public SpecificationFilterModel SpecificationFilter { get; set; }
		/// <summary>
		/// Manufacturer filter model
		/// </summary>
		public ManufacturerFilterModel ManufacturerFilter { get; set; }

		/// <summary>
		/// A value indicating whether product sorting is allowed
		/// </summary>
		public bool AllowProductSorting { get; set; }
		/// <summary>
		/// Available sort options
		/// </summary>
		public IList<SelectListItem> AvailableSortOptions { get; set; }

		/// <summary>
		/// A value indicating whether customers are allowed to change view mode
		/// </summary>
		public bool AllowProductViewModeChanging { get; set; }
		/// <summary>
		/// Available view mode options
		/// </summary>
		public IList<SelectListItem> AvailableViewModes { get; set; }

		/// <summary>
		/// A value indicating whether customers are allowed to select page size
		/// </summary>
		public bool AllowCustomersToSelectPageSize { get; set; }
		/// <summary>
		/// Available page size options
		/// </summary>
		public IList<SelectListItem> PageSizeOptions { get; set; }

		/// <summary>
		/// Order by
		/// </summary>
		public int? OrderBy { get; set; }

		/// <summary>
		/// Product sorting
		/// </summary>
		public string ViewMode { get; set; }

		#endregion

		#region Nested classes

		/// <summary>
		/// Price range filter model
		/// </summary>
		public partial class PriceRangeFilterModel : BaseNopModel
		{
			#region Const

			private const string QUERYSTRINGPARAM = "price";
			private const decimal FORMAT_PRICE_PATTERN = 100000.00M;

			#endregion

			#region Ctor

			/// <summary>
			/// Constuctor
			/// </summary>
			public PriceRangeFilterModel()
			{
				this.Items = new List<PriceRangeFilterItem>();
			}

			#endregion

			#region Utilities

			/// <summary>
			/// Gets parsed price ranges
			/// </summary>
			/// <param name="priceRangesStr">Price ranges in string format</param>
			/// <returns>Price ranges</returns>
			protected virtual IList<PriceRange> GetPriceRangeList(string priceRangesStr)
			{
				var priceRanges = new List<PriceRange>();
				if (string.IsNullOrWhiteSpace(priceRangesStr))
					return priceRanges;
				string[] rangeArray = priceRangesStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string str1 in rangeArray)
				{
					string[] fromTo = str1.Trim().Split(new[] { '-' });

					decimal? from = null;
					if (!String.IsNullOrEmpty(fromTo[0]) && !String.IsNullOrEmpty(fromTo[0].Trim()))
						from = decimal.Parse(fromTo[0].Trim(), new CultureInfo("en-US"));

					decimal? to = null;
					if (!String.IsNullOrEmpty(fromTo[1]) && !String.IsNullOrEmpty(fromTo[1].Trim()))
						to = decimal.Parse(fromTo[1].Trim(), new CultureInfo("en-US"));

					priceRanges.Add(new PriceRange { From = from, To = to });
				}
				return priceRanges;
			}

			/// <summary>
			/// Exclude query string parameters
			/// </summary>
			/// <param name="url">URL</param>
			/// <param name="webHelper">Web helper</param>
			/// <returns>New URL</returns>
			protected virtual string ExcludeQueryStringParams(string url, IWebHelper webHelper)
			{
				//comma separated list of parameters to exclude
				const string excludedQueryStringParams = "pagenumber";
				var excludedQueryStringParamsSplitted = excludedQueryStringParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string exclude in excludedQueryStringParamsSplitted)
					url = webHelper.RemoveQueryString(url, exclude);
				return url;
			}

			#endregion

			#region Methods

			/// <summary>
			/// Get selected price range
			/// </summary>
			/// <param name="webHelper">Web helper</param>
			/// <param name="priceRangesStr">Price ranges in string format</param>
			/// <returns>Price ranges</returns>
			public virtual PriceRange GetSelectedPriceRange(IWebHelper webHelper, string priceRangesStr)
			{
				//_currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase, _workContext.WorkingCurrency);
				var range = webHelper.QueryString<string>(QUERYSTRINGPARAM);
				if (String.IsNullOrEmpty(range))
					return null;
				string[] fromTo = range.Trim().Split(new[] { '-' });

				if (fromTo.Length == 2)
				{
					decimal? from = null;
					if (!String.IsNullOrEmpty(fromTo[0]) && !String.IsNullOrEmpty(fromTo[0].Trim()))
						from = decimal.Parse(fromTo[0].Trim(), new CultureInfo("en-US"));
					decimal? to = null;
					if (!String.IsNullOrEmpty(fromTo[1]) && !String.IsNullOrEmpty(fromTo[1].Trim()))
						to = decimal.Parse(fromTo[1].Trim(), new CultureInfo("en-US"));

					return new PriceRange { From = from, To = to };
				}

				return null;
			}

			/// <summary>
			/// Load price range filters
			/// </summary>
			/// <param name="priceRangeStr">Price range in string format</param>
			/// <param name="webHelper">Web helper</param>
			/// <param name="priceFormatter">Price formatter</param>
			public virtual void LoadPriceRangeFilters(string priceRangeStr, IWebHelper webHelper, IPriceFormatter priceFormatter)
			{
				var priceRangeList = GetPriceRangeList(priceRangeStr);
				if (priceRangeList.Any())
				{
					this.Enabled = true;

					var selectedPriceRange = GetSelectedPriceRange(webHelper, priceRangeStr);

					this.Items = priceRangeList.ToList().Select(x =>
					{
						//from&to
						var item = new PriceRangeFilterItem();
						if (x.From.HasValue)
							item.From = priceFormatter.FormatPrice(x.From.Value, true, false);
						if (x.To.HasValue)
							item.To = priceFormatter.FormatPrice(x.To.Value, true, false);
						string fromQuery = string.Empty;
						if (x.From.HasValue)
							fromQuery = x.From.Value.ToString(new CultureInfo("en-US"));
						string toQuery = string.Empty;
						if (x.To.HasValue)
							toQuery = x.To.Value.ToString(new CultureInfo("en-US"));

						//is selected?
						if (selectedPriceRange != null
							&& selectedPriceRange.From == x.From
							&& selectedPriceRange.To == x.To)
							item.Selected = true;

						//filter URL
						string url = webHelper.ModifyQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM + "=" + fromQuery + "-" + toQuery, null);
						url = ExcludeQueryStringParams(url, webHelper);
						item.FilterUrl = url;


						return item;
					}).ToList();

					if (selectedPriceRange != null)
					{
						//remove filter URL
						string url = webHelper.RemoveQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM);
						url = ExcludeQueryStringParams(url, webHelper);
						this.RemoveFilterUrl = url;
					}
				}
				else
				{
					this.Enabled = false;
				}
			}

			public virtual void LoadPriceRangeFiltersByCategory(
				int categoryId,
				IPriceRangeService priceRangeService,
				IWebHelper webHelper,
				IPriceFormatter priceFormatter)
			{
				var priceRange = priceRangeService.GetPriceRangeByCategory(categoryId);
				if (priceRange == null) return;

				this.Enabled = true;
				this.PriceFormat = priceFormatter.FormatPrice(FORMAT_PRICE_PATTERN, true, false);
				this.Items.Add(new PriceRangeFilterItem
					{
						From = priceFormatter.FormatPrice(priceRange.From.Value, true, false),
						To = priceFormatter.FormatPrice(priceRange.To.Value, true, false),
						FromDigit = priceRange.From.Value,
						ToDigit = priceRange.To.Value
					});

				var selectedPriceRange = GetSelectedPriceRange(webHelper, null);

				if (selectedPriceRange != null)
				{
					//remove filter URL
					string url = webHelper.RemoveQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM);
					url = ExcludeQueryStringParams(url, webHelper);
					this.RemoveFilterUrl = url;

					this.Items.Add(new PriceRangeFilterItem
					{
						From = priceFormatter.FormatPrice(selectedPriceRange.From.Value, true, false),
						To = priceFormatter.FormatPrice(selectedPriceRange.To.Value, true, false),
						FromDigit = selectedPriceRange.From.Value,
						ToDigit = selectedPriceRange.To.Value,
						Selected = true
					});
				}
			}

			#endregion

			#region Properties

			/// <summary>
			/// Gets or sets a value indicating whether filtering is enabled
			/// </summary>
			public bool Enabled { get; set; }

			/// <summary>
			/// Filter items
			/// </summary>
			public IList<PriceRangeFilterItem> Items { get; set; }

			public string PriceFormat { get; set; }

			/// <summary>
			/// URL of "remove filters" button
			/// </summary>
			public string RemoveFilterUrl { get; set; }

			#endregion
		}

		/// <summary>
		/// Price range filter item
		/// </summary>
		public partial class PriceRangeFilterItem : BaseNopModel
		{
			public decimal FromDigit { get; set; }

			public decimal ToDigit { get; set; }

			/// <summary>
			/// From
			/// </summary>
			public string From { get; set; }
			/// <summary>
			/// To
			/// </summary>
			public string To { get; set; }
			/// <summary>
			/// Filter URL
			/// </summary>
			public string FilterUrl { get; set; }
			/// <summary>
			/// Is selected?
			/// </summary>
			public bool Selected { get; set; }
		}

		/// <summary>
		/// Manufacturer filter view model
		/// </summary>
		public partial class ManufacturerFilterModel : BaseNopModel
		{
			#region Const

			private const string QUERYSTRINGPARAM = "mafr";

			#endregion

			public ManufacturerFilterModel()
			{
				AlreadyFilteredItems = new List<ManufacturerFilterItem>();
				NotFilteredItems = new List<ManufacturerFilterItem>();
			}

			#region Utilities

			/// <summary>
			/// Exclude query string parameters
			/// </summary>
			/// <param name="url">URL</param>
			/// <param name="webHelper">Web helper</param>
			/// <returns>New URL</returns>
			protected virtual string ExcludeQueryStringParams(string url, IWebHelper webHelper)
			{
				//comma separated list of parameters to exclude
				const string excludedQueryStringParams = "pagenumber";
				var excludedQueryStringParamsSplitted = excludedQueryStringParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string exclude in excludedQueryStringParamsSplitted)
					url = webHelper.RemoveQueryString(url, exclude);
				return url;
			}

			/// <summary>
			/// Generate URL of already filtered items
			/// </summary>
			/// <param name="optionIds">Option IDs</param>
			/// <returns>URL</returns>
			protected virtual string GenerateFilteredManufacturerQueryParam(IList<int> optionIds)
			{
				if (optionIds == null)
					return "";

				string result = string.Join(",", optionIds);
				return result;
			}

			#endregion

			#region Methods

			/// <summary>
			/// Get IDs of already filtered specification options
			/// </summary>
			/// <param name="webHelper">Web helper</param>
			/// <returns>IDs</returns>
			public virtual List<int> GetAlreadyFilteredManufacturerIds(IWebHelper webHelper)
			{
				var result = new List<int>();

				var alreadyFilteredManufacturersStr = webHelper.QueryString<string>(QUERYSTRINGPARAM);
				if (String.IsNullOrWhiteSpace(alreadyFilteredManufacturersStr))
					return result;

				foreach (var item in alreadyFilteredManufacturersStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					int mafrId;
					int.TryParse(item.Trim(), out mafrId);
					if (!result.Contains(mafrId))
						result.Add(mafrId);
				}
				return result;
			}

			/// <summary>
			/// Prepare model
			/// </summary>
			/// <param name="alreadyFilteredManufacturerIds">IDs of already filtered specification options</param>
			/// <param name="filterableSpecificationAttributeOptionIds">IDs of filterable specification options</param>
			/// <param name="specificationAttributeService"></param>
			/// <param name="webHelper">Web helper</param>
			/// <param name="workContext">Work context</param>
			/// <param name="cacheManager">Cache manager</param>
			public virtual void PrepareManufacturerFilters(
				int categoryId,
				IManufacturerService manufacturerService,
				IWebHelper webHelper,
				IWorkContext workContext,
				ICacheManager cacheManager)
			{
				Enabled = false;

				IList<int> alreadyFilteredManufacturerIds = this.GetAlreadyFilteredManufacturerIds(webHelper);

				var optionIds = alreadyFilteredManufacturerIds != null
					? string.Join(",", alreadyFilteredManufacturerIds) : string.Empty;

				var allManufacturersByCategory = manufacturerService.GetManufacturersByCategoryId(categoryId);

				if (!allManufacturersByCategory.Any())
					return;

				//prepare the model properties
				Enabled = true;
				var removeFilterUrl = webHelper.RemoveQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM);
				RemoveFilterUrl = ExcludeQueryStringParams(removeFilterUrl, webHelper);

				//get already filtered specification options
				var alreadyFilteredOptions = allManufacturersByCategory
					.Where(x => alreadyFilteredManufacturerIds.Contains(x.Id));

				AlreadyFilteredItems = alreadyFilteredOptions.Select(x =>
					new ManufacturerFilterItem
					{
						ManufacturerId = x.Id,
						ManufacturerName = x.Name
					}).ToList();

				//get not filtered specification options
				NotFilteredItems = allManufacturersByCategory.Select(x =>
				{
					//filter URL
					var alreadyFiltered = alreadyFilteredManufacturerIds.Concat(new List<int> { x.Id });
					var queryString = string.Format("{0}={1}", QUERYSTRINGPARAM, GenerateFilteredManufacturerQueryParam(alreadyFiltered.ToList()));
					var filterUrl = webHelper.ModifyQueryString(webHelper.GetThisPageUrl(true), queryString, null);

					return new ManufacturerFilterItem
					{
						ManufacturerId = x.Id,
						ManufacturerName = x.Name,
						FilterUrl = ExcludeQueryStringParams(filterUrl, webHelper)
					};
				}).ToList();
			}

			#endregion

			#region Properties
			/// <summary>
			/// Gets or sets a value indicating whether filtering is enabled
			/// </summary>
			public bool Enabled { get; set; }
			/// <summary>
			/// Already filtered items
			/// </summary>
			public IList<ManufacturerFilterItem> AlreadyFilteredItems { get; set; }
			/// <summary>
			/// Not filtered yet items
			/// </summary>
			public IList<ManufacturerFilterItem> NotFilteredItems { get; set; }
			/// <summary>
			/// URL of "remove filters" button
			/// </summary>
			public string RemoveFilterUrl { get; set; }

			#endregion
		}

		/// <summary>
		/// Manufacturer filter item
		/// </summary>
		public partial class ManufacturerFilterItem : BaseNopModel
		{
			/// <summary>
			/// Manufacturer name
			/// </summary>
			public string ManufacturerName { get; set; }

			/// <summary>
			/// Manufacturer Id
			/// </summary>
			public int ManufacturerId { get; set; }

			/// <summary>
			/// Filter URL
			/// </summary>
			public string FilterUrl { get; set; }
		}


		/// <summary>
		/// Specification filter model
		/// </summary>
		public partial class SpecificationFilterModel : BaseNopModel
		{
			#region Const

			private const string QUERYSTRINGPARAM = "specs";

			#endregion

			#region Ctor

			/// <summary>
			/// Ctor
			/// </summary>
			public SpecificationFilterModel()
			{
				this.AlreadyFilteredItems = new List<SpecificationFilterItem>();
				this.NotFilteredItems = new List<SpecificationFilterItem>();
			}

			#endregion

			#region Utilities

			/// <summary>
			/// Exclude query string parameters
			/// </summary>
			/// <param name="url">URL</param>
			/// <param name="webHelper">Web helper</param>
			/// <returns>New URL</returns>
			protected virtual string ExcludeQueryStringParams(string url, IWebHelper webHelper)
			{
				//comma separated list of parameters to exclude
				const string excludedQueryStringParams = "pagenumber";
				var excludedQueryStringParamsSplitted = excludedQueryStringParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string exclude in excludedQueryStringParamsSplitted)
					url = webHelper.RemoveQueryString(url, exclude);
				return url;
			}

			/// <summary>
			/// Generate URL of already filtered items
			/// </summary>
			/// <param name="optionIds">Option IDs</param>
			/// <returns>URL</returns>
			protected virtual string GenerateFilteredSpecQueryParam(IList<int> optionIds)
			{
				if (optionIds == null)
					return "";

				string result = string.Join(",", optionIds);
				return result;
			}

			#endregion

			#region Methods

			/// <summary>
			/// Get IDs of already filtered specification options
			/// </summary>
			/// <param name="webHelper">Web helper</param>
			/// <returns>IDs</returns>
			public virtual List<int> GetAlreadyFilteredSpecOptionIds(IWebHelper webHelper)
			{
				var result = new List<int>();

				var alreadyFilteredSpecsStr = webHelper.QueryString<string>(QUERYSTRINGPARAM);
				if (String.IsNullOrWhiteSpace(alreadyFilteredSpecsStr))
					return result;

				foreach (var spec in alreadyFilteredSpecsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					int specId;
					int.TryParse(spec.Trim(), out specId);
					if (!result.Contains(specId))
						result.Add(specId);
				}
				return result;
			}

			/// <summary>
			/// Prepare model
			/// </summary>
			/// <param name="alreadyFilteredSpecOptionIds">IDs of already filtered specification options</param>
			/// <param name="filterableSpecificationAttributeOptionIds">IDs of filterable specification options</param>
			/// <param name="specificationAttributeService"></param>
			/// <param name="webHelper">Web helper</param>
			/// <param name="workContext">Work context</param>
			/// <param name="cacheManager">Cache manager</param>
			public virtual void PrepareSpecsFilters(IList<int> alreadyFilteredSpecOptionIds,
				int[] filterableSpecificationAttributeOptionIds,
				ISpecificationAttributeService specificationAttributeService,
				IWebHelper webHelper, IWorkContext workContext, ICacheManager cacheManager)
			{
				Enabled = false;
				var optionIds = filterableSpecificationAttributeOptionIds != null
					? string.Join(",", filterableSpecificationAttributeOptionIds) : string.Empty;
				var cacheKey = string.Format(ModelCacheEventConsumer.SPECS_FILTER_MODEL_KEY, optionIds, workContext.WorkingLanguage.Id);

				var allOptions = specificationAttributeService.GetSpecificationAttributeOptionsByIds(filterableSpecificationAttributeOptionIds);
				var allFilters = cacheManager.Get(cacheKey, () => allOptions.Select(sao =>
					new SpecificationAttributeOptionFilter
					{
						SpecificationAttributeId = sao.SpecificationAttribute.Id,
						SpecificationAttributeName = sao.SpecificationAttribute.GetLocalized(x => x.Name, workContext.WorkingLanguage.Id),
						SpecificationAttributeDisplayOrder = sao.SpecificationAttribute.DisplayOrder,
						SpecificationAttributeOptionId = sao.Id,
						SpecificationAttributeOptionName = sao.GetLocalized(x => x.Name, workContext.WorkingLanguage.Id),
						SpecificationAttributeOptionColorRgb = sao.ColorSquaresRgb,
						SpecificationAttributeOptionDisplayOrder = sao.DisplayOrder
					}).ToList());

				if (!allFilters.Any())
					return;

				//sort loaded options
				allFilters = allFilters.OrderBy(saof => saof.SpecificationAttributeDisplayOrder)
					.ThenBy(saof => saof.SpecificationAttributeName)
					.ThenBy(saof => saof.SpecificationAttributeOptionDisplayOrder)
					.ThenBy(saof => saof.SpecificationAttributeOptionName).ToList();

				//prepare the model properties
				Enabled = true;
				var removeFilterUrl = webHelper.RemoveQueryString(webHelper.GetThisPageUrl(true), QUERYSTRINGPARAM);
				RemoveFilterUrl = ExcludeQueryStringParams(removeFilterUrl, webHelper);

				//get already filtered specification options
				var alreadyFilteredOptions = allFilters.Where(x => alreadyFilteredSpecOptionIds.Contains(x.SpecificationAttributeOptionId));
				AlreadyFilteredItems = alreadyFilteredOptions.Select(x =>
					new SpecificationFilterItem
					{
						SpecificationAttributeName = x.SpecificationAttributeName,
						SpecificationAttributeOptionName = x.SpecificationAttributeOptionName,
						SpecificationAttributeOptionColorRgb = x.SpecificationAttributeOptionColorRgb
					}).ToList();

				//get not filtered specification options
				NotFilteredItems = allFilters.Except(alreadyFilteredOptions).Select(x =>
				{
					//filter URL
					var alreadyFiltered = alreadyFilteredSpecOptionIds.Concat(new List<int> { x.SpecificationAttributeOptionId });
					var queryString = string.Format("{0}={1}", QUERYSTRINGPARAM, GenerateFilteredSpecQueryParam(alreadyFiltered.ToList()));
					var filterUrl = webHelper.ModifyQueryString(webHelper.GetThisPageUrl(true), queryString, null);

					return new SpecificationFilterItem()
					{
						SpecificationAttributeName = x.SpecificationAttributeName,
						SpecificationAttributeOptionName = x.SpecificationAttributeOptionName,
						SpecificationAttributeOptionColorRgb = x.SpecificationAttributeOptionColorRgb,
						FilterUrl = ExcludeQueryStringParams(filterUrl, webHelper)
					};
				}).ToList();
			}

			#endregion

			#region Properties
			/// <summary>
			/// Gets or sets a value indicating whether filtering is enabled
			/// </summary>
			public bool Enabled { get; set; }
			/// <summary>
			/// Already filtered items
			/// </summary>
			public IList<SpecificationFilterItem> AlreadyFilteredItems { get; set; }
			/// <summary>
			/// Not filtered yet items
			/// </summary>
			public IList<SpecificationFilterItem> NotFilteredItems { get; set; }
			/// <summary>
			/// URL of "remove filters" button
			/// </summary>
			public string RemoveFilterUrl { get; set; }

			#endregion
		}

		/// <summary>
		/// Specification filter item
		/// </summary>
		public partial class SpecificationFilterItem : BaseNopModel
		{
			/// <summary>
			/// Specification attribute name
			/// </summary>
			public string SpecificationAttributeName { get; set; }
			/// <summary>
			/// Specification attribute option name
			/// </summary>
			public string SpecificationAttributeOptionName { get; set; }
			/// <summary>
			/// Specification attribute option color (RGB)
			/// </summary>
			public string SpecificationAttributeOptionColorRgb { get; set; }
			/// <summary>
			/// Filter URL
			/// </summary>
			public string FilterUrl { get; set; }
		}

		#endregion
	}
}