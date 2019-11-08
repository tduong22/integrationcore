using Ardalis.GuardClauses;
using Autofac;
using AutoMapper;
using System.Collections.Generic;

namespace Comvita.Common.Actor.DI
{
    public class AutoMapperModule : Module
    {
        public List<Profile> ListOfMappingProfile {get;set; }

        protected override void Load(ContainerBuilder builder) {

            Guard.Against.Null(ListOfMappingProfile, nameof(ListOfMappingProfile));

            builder.Register(c => new MapperConfiguration(cfg =>
            {
                foreach (var profile in ListOfMappingProfile)
                {
                    cfg.AddProfile(profile);
                }

            })).SingleInstance();
            builder.Register(c => c.Resolve<MapperConfiguration>().CreateMapper(c.Resolve)).As<IMapper>().SingleInstance();
        }
    }
}
