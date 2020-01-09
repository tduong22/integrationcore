using Integration.Common.Exceptions;
using Integration.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Integration.Common.Model
{
    [DataContract]
    public class OrchestrationOrderCollection
    {
        //action name
        [DataMember]
        public string Name { get; set; }

        private bool _isEmpty;

        [DataMember]
        public bool IsEmpty
        {
            get => OrchestrationOrders == null || OrchestrationOrders.Count == 0;

            set => _isEmpty = value;
        }

        [DataMember(Name = "OrchestrationOrders")]
        public List<OrchestrationOrder> OrchestrationOrders { get; set; }

        public OrchestrationOrderCollection()
        {
            OrchestrationOrders = new List<OrchestrationOrder>();
        }

        public OrchestrationOrderCollection(string orderName) : base()
        {
            Name = orderName;
        }
        /// <summary>
        /// Return an executable order from the logic orchestrationOrder. Only make the call with executable order. 
        /// </summary>
        /// <returns></returns>
        public ExecutableOrchestrationOrder GetFirstExecutableOrder()
        {
            if (OrchestrationOrders.Count > 1)
            {
                throw new OrchestrationOrderCollectionMultipleOrderException(
                    $"Failed to get a single order on an array of orders under name {Name}. Please call this operation on an single order collection.");
            }
            var order = OrchestrationOrders.First();

            return order.ToExecutableOrder();
        }

        public OrchestrationOrder GetSingleOrder(Predicate<OrchestrationOrder> predicate)
        {
            return OrchestrationOrders.Find(predicate);
        }



        public OrchestrationOrder GetFirstOrder()
        {
            return OrchestrationOrders.FirstOrDefault();
        }

        public IEnumerable<OrchestrationOrder> GetOrchestrationOrders(Predicate<OrchestrationOrder> predicate)
        {
            return OrchestrationOrders.FindAll(predicate);
        }

        public OrchestrationOrder CreateSingleOrder(string orderName, OrchestrationOrder orchestrationOrder)
        {
            Name = orderName;
            if (OrchestrationOrders == null)
            {
                throw new OrchestrationOrderCollectionNullException(
                    $"Failed to create a single order on this {orderName}");
            }
            OrchestrationOrders.Add(orchestrationOrder);
            return orchestrationOrder;
        }

        public static OrchestrationOrderCollection NoOrder()
        {
            return new OrchestrationOrderCollection();
        }
    }
}