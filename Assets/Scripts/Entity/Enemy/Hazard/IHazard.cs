using System; 
namespace SQZL.Entity.Enemy.Hazard
{
    public interface IHazard
    {
        public event Action<IHazard> OnHazardTripped;
        public event Action<IHazard> OnHazardDestroyed;
        
        internal void TripHazard(HazardTripBox trigger);
    }
}
