# Activity Diagram Web Khách Hàng

File StarUML chính: `e-commerce-customer-activity.mdj`

## Danh sách sơ đồ
- `01 Đăng nhập và đăng ký tài khoản`: Khách mở trang đăng nhập hoặc đăng ký, chọn cách xác thực, hệ thống kiểm tra thông tin và ghi nhớ trạng thái đăng nhập để khách quay lại trang mong muốn.
- `02 Tìm và xem sản phẩm`: Khách tìm sản phẩm từ trang chủ, menu danh mục hoặc ô tìm kiếm, xem danh sách phù hợp rồi mở trang chi tiết để chọn màu, phiên bản và xem tình trạng còn hàng.
- `03 Chọn sản phẩm để mua`: Khách chọn phiên bản sản phẩm, thêm vào giỏ hoặc mua ngay; hệ thống kiểm tra sản phẩm còn bán, còn hàng rồi cập nhật giỏ hoặc chuyển sang bước đặt hàng.
- `04 Quản lý giỏ hàng và chọn món thanh toán`: Khách mở giỏ hàng, chỉnh số lượng, xóa món, chọn món muốn thanh toán; hệ thống chỉ ghi nhớ các món đã chọn để đặt hàng, không làm mất những món chưa chọn.
- `05 Đặt hàng thanh toán khi nhận hàng`: Khách nhập thông tin nhận hàng, chọn thanh toán khi nhận hàng, áp dụng ưu đãi nếu có; hệ thống kiểm tra đơn, lưu đơn và hiển thị trang đặt hàng thành công.
- `06 Đặt hàng qua ví hoặc ngân hàng`: Khách chọn thanh toán qua ví hoặc ngân hàng, được chuyển sang cổng thanh toán; sau khi có kết quả, hệ thống xác nhận đơn thành công hoặc hủy đơn thất bại.
- `07 Chuyển khoản SePay và xác nhận giao dịch`: Khách chọn chuyển khoản SePay, xem mã QR và nội dung chuyển khoản; hệ thống tự đối chiếu giao dịch từ ngân hàng rồi cập nhật trạng thái đơn hàng.
- `08 Xem hồ sơ và quản lý địa chỉ`: Khách vào hồ sơ để xem thông tin cá nhân, đơn hàng, địa chỉ và sản phẩm yêu thích; khách có thể thêm địa chỉ mới hoặc đặt địa chỉ mặc định.
- `09 Theo dõi đơn hàng và gửi đánh giá`: Khách mở chi tiết đơn để theo dõi trạng thái, vận chuyển và chỉ gửi đánh giá khi đơn đã hoàn tất, đã thanh toán và đúng sản phẩm đã mua.
- `10 Lưu sản phẩm yêu thích`: Khách bấm yêu thích trên sản phẩm hoặc xóa sản phẩm khỏi danh sách yêu thích; nếu chưa đăng nhập, hệ thống mời khách đăng nhập trước.
- `11 Nhắn tin với nhân viên hỗ trợ`: Khách mở khung hỗ trợ, đăng nhập nếu cần, gửi tin nhắn cho nhân viên và nhận phản hồi ngay trên màn hình trò chuyện.
- `12 Hỏi trợ lý tư vấn sản phẩm`: Khách hỏi trợ lý tư vấn sản phẩm; hệ thống tìm sản phẩm phù hợp, tạo câu trả lời dễ hiểu và lưu lịch sử tư vấn khi khách đã đăng nhập.

## Căn cứ trình bày luồng
- Các bước khách nhìn thấy và thao tác trực tiếp trên màn hình.
- Các phản hồi mà hệ thống hiển thị lại cho khách khi thành công, thiếu thông tin hoặc có lỗi.
- Các nhánh đặc biệt trong mua hàng: mua ngay, chọn một phần giỏ hàng, thanh toán khi nhận hàng, thanh toán online và chuyển khoản.
- Các luồng sau mua: xem đơn hàng, theo dõi vận chuyển, gửi đánh giá, lưu sản phẩm yêu thích, nhắn tin hỗ trợ và hỏi trợ lý tư vấn.

## Ghi chú khi đọc sơ đồ
- Ngôn ngữ trong sơ đồ ưu tiên cách nói tự nhiên để người không biết kỹ thuật vẫn đọc được.
- Giỏ hàng và danh sách món được chọn để thanh toán được hiểu là hai bước khác nhau: chọn món để đặt hàng không làm mất món còn lại trong giỏ.
- Với thanh toán online, đơn được tạo trước rồi hệ thống chờ kết quả thanh toán để xác nhận hoặc hủy đơn.
- Với chuyển khoản SePay, khách xem hướng dẫn chuyển khoản, còn hệ thống tự đối chiếu giao dịch ngân hàng để cập nhật đơn.
- Đánh giá sau mua chỉ xuất hiện khi đơn đã hoàn tất và đúng sản phẩm khách đã mua.
