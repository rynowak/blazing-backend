namespace BlazingPizza
{
    public class OrderStatus
    {
        public int Id { get; set; }

        public LatLong CurrentLocation { get; set; }
        
        public string Status { get; set; }
    }
}