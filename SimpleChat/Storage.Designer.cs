﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Data.EntityClient;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;

[assembly: EdmSchemaAttribute()]

namespace SimpleChat
{
    #region Contexts
    
    /// <summary>
    /// No Metadata Documentation available.
    /// </summary>
    public partial class StorageContainer : ObjectContext
    {
        #region Constructors
    
        /// <summary>
        /// Initializes a new StorageContainer object using the connection string found in the 'StorageContainer' section of the application configuration file.
        /// </summary>
        public StorageContainer() : base("name=StorageContainer", "StorageContainer")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new StorageContainer object.
        /// </summary>
        public StorageContainer(string connectionString) : base(connectionString, "StorageContainer")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Initialize a new StorageContainer object.
        /// </summary>
        public StorageContainer(EntityConnection connection) : base(connection, "StorageContainer")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        #endregion
    
        #region Partial Methods
    
        partial void OnContextCreated();
    
        #endregion
    
    }
    

    #endregion
    
    
}