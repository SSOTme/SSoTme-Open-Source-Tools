using System;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using CoreLibrary.Extensions;

namespace SSoTme.OST.Lib.DataClasses
{                            
    public partial class ProjectSetting
    {
        private void InitPoco()
        {
            
            this.ProjectSettingId = Guid.NewGuid();
            

        }

        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, PropertyName = "ProjectSettingId")]
        public Guid ProjectSettingId { get; set; }
    
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, PropertyName = "SSoTmeProjectId")]
        public Guid SSoTmeProjectId { get; set; }
    
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, PropertyName = "Name")]
        public String Name { get; set; }
    
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, PropertyName = "Value")]
        public String Value { get; set; }
    

        

        

        private static string CreateProjectSettingWhere(IEnumerable<ProjectSetting> projectSettings)
        {
            if (!projectSettings.Any()) return "1=1";
            else 
            {
                var idList = projectSettings.Select(selectProjectSetting => String.Format("'{0}'", selectProjectSetting.ProjectSettingId));
                var csIdList = String.Join(",", idList);
                return String.Format("ProjectSettingId in ({0})", csIdList);
            }
        }
        
    }
}