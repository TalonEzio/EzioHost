export function renderPaypalButton(id, blazorCallbackUrl) {
    const paypalButtonId = `#${id}`;

    if (!window.paypal) {
        console.error("PayPal SDK chưa được load!");
        return;
    }

    window.paypal.Buttons({
        createOrder: function (data, actions) {
            return actions.order.create({
                purchase_units: [{
                    amount: {
                        value: '10.00' // Giá trị đơn hàng, có thể thay đổi theo gói VIP
                    }
                }]
            });
        },
        onApprove: function (data, actions) {
            console.log(data);
            return actions.order.capture().then(function (details) {
                console.log("Thanh toán thành công!", details);
                // Gửi request lên Blazor (hoặc trực tiếp tới reverse proxy)
                //fetch(blazorCallbackUrl, {
                //    method: "POST",
                //    headers: {
                //        "Content-Type": "application/json"
                //    },
                //    body: JSON.stringify({
                //        orderId: details.id,
                //        payerEmail: details.payer.email_address,
                //        status: details.status
                //    })
                //}).then(response => {
                //    if (!response.ok) {
                //        console.error("Lỗi khi gọi API Blazor!");
                //    }
                //}).catch(error => console.error("Lỗi kết nối tới Blazor:", error));
            });
        }
    }).render(paypalButtonId);
}