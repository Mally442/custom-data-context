# custom-data-context

This is a partial class I wrote to extend the functionality of an auto-generated Entity Framework 6 Database Context.  
It sets the values of logging fields (i.e. CreatedOn, CreatedBy, ModifiedOn, ModifiedBy) of EF entities that implement the custom ILoggable interface.