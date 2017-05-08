NOTE: 
The Tableau Workbook in this directory (SiteInventory.twb) is used as a template from which the Site Inventory Workbookis generated.  

If you want to edit the Tableau Workbook, MAKE SURE you have it point to a sample *.csv file named 'siteInventoryTemplate.csv'.  
The TabMigrate code that generates the Site Inventory workbook looks for and replaces the data file references to 
'siteInventoryTemplate.csv' in this template workbook. If the Workbook is saved with a reference to another file name the search/replace 
will not work and the result will be a Tableau Workbook that does NOT point to the data of the exported site.
