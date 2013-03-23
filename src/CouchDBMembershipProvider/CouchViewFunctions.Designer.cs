﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CouchDBMembershipProvider {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class CouchViewFunctions {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CouchViewFunctions() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CouchDBMembershipProvider.CouchViewFunctions", typeof(CouchViewFunctions).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function(doc) { 
        ///	if(doc.type == &apos;CouchDBMembershipProvider.User&apos;) 
        ///		emit([doc.email, doc.applicationName], doc.id) 
        ///}.
        /// </summary>
        internal static string byEmailAndAppName {
            get {
                return ResourceManager.GetString("byEmailAndAppName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function(doc) { 	
        ///	if(doc.type == &apos;CouchDBMembershipProvider.User&apos;) 
        ///		emit([doc.email, doc.applicationName, _count], doc);
        ///}.
        /// </summary>
        internal static string byEmailAndAppNameAndRowCount {
            get {
                return ResourceManager.GetString("byEmailAndAppNameAndRowCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function(doc) { 
        ///	if(doc.type == &apos;CouchDBMembershipProvider.User&apos;) 
        ///		emit([doc.username, doc.applicationName], doc) 
        ///}.
        /// </summary>
        internal static string byUserNameAndAppName {
            get {
                return ResourceManager.GetString("byUserNameAndAppName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function(doc) { 
        ///	if(doc.type == &apos;CouchDBMembershipProvider.User&apos;) 
        ///		emit([doc.username, doc.applicationName], true) 
        ///}.
        /// </summary>
        internal static string byUserNameAndAppNameExists {
            get {
                return ResourceManager.GetString("byUserNameAndAppNameExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function(doc) { 
        ///	if(doc.type == &apos;CouchDBMembershipProvider.User&apos;) 
        ///		emit(doc.username, doc) 
        ///}.
        /// </summary>
        internal static string byUserNameMap {
            get {
                return ResourceManager.GetString("byUserNameMap", resourceCulture);
            }
        }
    }
}
