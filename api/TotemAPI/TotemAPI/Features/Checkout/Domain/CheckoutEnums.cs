namespace TotemAPI.Features.Checkout.Domain;

public enum OrderFulfillment
{
    DineIn = 1,
    TakeAway = 2
}

public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    Pix = 3,
    Cash = 4
}

public enum OrderStatus
{
    Created = 1,
    Paid = 2,
    Cancelled = 3
}

public enum OrderKitchenStatus
{
    PendingPayment = 1,
    Queued = 2,
    InPreparation = 3,
    Ready = 4,
    Completed = 5,
    Cancelled = 6
}

public enum PaymentStatus
{
    Pending = 1,
    Approved = 2,
    Declined = 3
}
