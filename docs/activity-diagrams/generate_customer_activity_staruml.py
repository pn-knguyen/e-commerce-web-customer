# -*- coding: utf-8 -*-
"""Generate StarUML activity diagrams for the customer storefront."""

from __future__ import annotations

import json
import math
import re
import unicodedata
from dataclasses import dataclass
from pathlib import Path


BASE_DIR = Path(__file__).resolve().parent
MDJ_PATH = BASE_DIR / "e-commerce-customer-activity.mdj"
MD_PATH = BASE_DIR / "e-commerce-customer-activity.md"

PROJECT_ID = "PROJECT_ECOMMERCE_CUSTOMER_ACTIVITY"
MODEL_ID = "MODEL_CUSTOMER_ACTIVITY"

LANE_LEFT = 40
LANE_TOP = 40
LANE_WIDTH = 300
LANE_GAP = 20
LANE_HEADER = 34
STEP_Y = 82
NODE_TOP = LANE_TOP + LANE_HEADER + 30
ACTION_MIN_WIDTH = 88
ACTION_MAX_WIDTH = 238
ACTION_TEXT_PADDING = 28
ACTION_HEIGHT = 42
CONTROL_SIZE = 30
DECISION_WIDTH = 44
DECISION_HEIGHT = 34
RECTILINEAR_LINE_STYLE = 0


def slug(value: str) -> str:
    normalized = unicodedata.normalize("NFKD", value)
    ascii_text = normalized.encode("ascii", "ignore").decode("ascii")
    cleaned = re.sub(r"[^a-zA-Z0-9]+", "_", ascii_text.strip().lower()).strip("_")
    return cleaned or "item"


def ref(element_id: str) -> dict[str, str]:
    return {"$ref": element_id}


class IdFactory:
    def __init__(self) -> None:
        self.counts: dict[str, int] = {}

    def new(self, prefix: str, key: str = "") -> str:
        base = f"{prefix}_{slug(key)}" if key else prefix
        count = self.counts.get(base, 0) + 1
        self.counts[base] = count
        return f"{base}_{count:03d}"


ids = IdFactory()


@dataclass(frozen=True)
class NodeSpec:
    key: str
    kind: str
    lane: str
    name: str = ""


@dataclass(frozen=True)
class EdgeSpec:
    source: str
    target: str
    guard: str = ""


@dataclass(frozen=True)
class FlowSpec:
    key: str
    name: str
    documentation: str
    lanes: tuple[str, ...]
    nodes: tuple[NodeSpec, ...]
    edges: tuple[EdgeSpec, ...]


def n(key: str, kind: str, lane: str, name: str = "") -> NodeSpec:
    return NodeSpec(key, kind, lane, name)


def e(source: str, target: str, guard: str = "") -> EdgeSpec:
    return EdgeSpec(source, target, guard)


CUSTOMER = "Khách hàng"
WEB = "Màn hình web"
BACKEND = "Hệ thống cửa hàng"
EXTERNAL = "Dịch vụ bên ngoài"
ADMIN = "Nhân viên hỗ trợ"


FLOWS: tuple[FlowSpec, ...] = (
    FlowSpec(
        key="auth_firebase_sync",
        name="01 Đăng nhập, đăng ký và đồng bộ Firebase",
        documentation=(
            "Luồng xác thực khách hàng: khách chọn phương thức Firebase, web gửi "
            "ID token về /Account/FirebaseSync, backend verify token, tạo hoặc đọc "
            "hồ sơ khách hàng rồi lưu session."
        ),
        lanes=(CUSTOMER, WEB, BACKEND, EXTERNAL),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_login", "action", CUSTOMER, "Mở đăng nhập / đăng ký"),
            n("choose_method", "action", CUSTOMER, "Chọn email, social, OTP hoặc magic link"),
            n("firebase_auth", "action", EXTERNAL, "Firebase xác thực"),
            n("has_token", "decision", EXTERNAL, "Có ID token?"),
            n("auth_error", "action", CUSTOMER, "Hiển thị lỗi và nhập lại"),
            n("sync_backend", "action", WEB, "Gửi token đến /Account/FirebaseSync"),
            n("verify_token", "action", BACKEND, "Verify ID token bằng Firebase Admin"),
            n("valid_token", "decision", BACKEND, "Token hợp lệ?"),
            n("resolve_identity", "action", BACKEND, "Xác định email từ Firebase hoặc phone"),
            n("profile_exists", "decision", BACKEND, "Hồ sơ đã tồn tại?"),
            n("create_profile", "action", BACKEND, "Tạo hồ sơ khách hàng mới"),
            n("load_profile", "action", BACKEND, "Đọc hồ sơ khách hàng"),
            n("save_session", "action", WEB, "Lưu session đăng nhập"),
            n("redirect", "action", WEB, "Chuyển về returnUrl hoặc Home"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_login"),
            e("open_login", "choose_method"),
            e("choose_method", "firebase_auth"),
            e("firebase_auth", "has_token"),
            e("has_token", "auth_error", "Không"),
            e("auth_error", "choose_method", "Thử lại"),
            e("has_token", "sync_backend", "Có"),
            e("sync_backend", "verify_token"),
            e("verify_token", "valid_token"),
            e("valid_token", "auth_error", "Không hợp lệ"),
            e("valid_token", "resolve_identity", "Hợp lệ"),
            e("resolve_identity", "profile_exists"),
            e("profile_exists", "create_profile", "Chưa có"),
            e("create_profile", "load_profile"),
            e("profile_exists", "load_profile", "Đã có"),
            e("load_profile", "save_session"),
            e("save_session", "redirect"),
            e("redirect", "end"),
        ),
    ),
    FlowSpec(
        key="catalog_search_product",
        name="02 Duyệt catalog, tìm kiếm và xem sản phẩm",
        documentation=(
            "Luồng khám phá sản phẩm từ trang chủ, menu danh mục, search overlay, "
            "catalog lọc/sắp xếp và trang chi tiết sản phẩm."
        ),
        lanes=(CUSTOMER, WEB, BACKEND),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_entry", "action", CUSTOMER, "Mở trang chủ, menu hoặc ô tìm kiếm"),
            n("type_query", "action", CUSTOMER, "Nhập từ khóa hoặc chọn danh mục"),
            n("suggest", "action", WEB, "Gọi /search/suggest khi cần gợi ý"),
            n("read_suggestion", "action", BACKEND, "Tìm gợi ý và sản phẩm nhanh"),
            n("submit_navigation", "action", CUSTOMER, "Chọn gợi ý, search hoặc catalog"),
            n("send_request", "action", WEB, "Gửi /search hoặc /catalog"),
            n("normalize_query", "action", BACKEND, "Chuẩn hóa slug, brand, sort, f_* filters"),
            n("build_listing", "action", BACKEND, "Tạo model catalog/search từ database"),
            n("listing_found", "decision", BACKEND, "Có dữ liệu trang?"),
            n("not_found", "action", WEB, "Trả trang 404"),
            n("render_listing", "action", WEB, "Render danh sách sản phẩm"),
            n("sectioned", "decision", WEB, "Trang theo section?"),
            n("load_section", "action", WEB, "AJAX /catalog/section-products khi sort section"),
            n("choose_product", "action", CUSTOMER, "Chọn sản phẩm"),
            n("request_detail", "action", WEB, "Gửi /product/{slug}?variant=..."),
            n("load_detail", "action", BACKEND, "Đọc chi tiết, biến thể, gallery, review, upsell"),
            n("product_found", "decision", BACKEND, "Sản phẩm tồn tại?"),
            n("product_404", "action", WEB, "Trả trang 404 sản phẩm"),
            n("render_detail", "action", WEB, "Render trang chi tiết"),
            n("select_variant", "action", CUSTOMER, "Chọn màu / phiên bản"),
            n("sync_variant_ui", "action", WEB, "Cập nhật ảnh, URL, giá, tồn kho, nút mua"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_entry"),
            e("open_entry", "type_query"),
            e("type_query", "suggest", "Cần gợi ý"),
            e("suggest", "read_suggestion"),
            e("read_suggestion", "submit_navigation"),
            e("type_query", "submit_navigation", "Đi thẳng"),
            e("submit_navigation", "send_request"),
            e("send_request", "normalize_query"),
            e("normalize_query", "build_listing"),
            e("build_listing", "listing_found"),
            e("listing_found", "not_found", "Không"),
            e("not_found", "end"),
            e("listing_found", "render_listing", "Có"),
            e("render_listing", "sectioned"),
            e("sectioned", "load_section", "Có"),
            e("load_section", "render_listing", "Cập nhật section"),
            e("sectioned", "choose_product", "Không / đã chọn"),
            e("choose_product", "request_detail"),
            e("request_detail", "load_detail"),
            e("load_detail", "product_found"),
            e("product_found", "product_404", "Không"),
            e("product_404", "end"),
            e("product_found", "render_detail", "Có"),
            e("render_detail", "select_variant"),
            e("select_variant", "sync_variant_ui"),
            e("sync_variant_ui", "end"),
        ),
    ),
    FlowSpec(
        key="product_cart_buy_now",
        name="03 Thêm vào giỏ hoặc mua ngay từ chi tiết sản phẩm",
        documentation=(
            "Luồng thao tác mua từ trang chi tiết: thêm vào giỏ, mua ngay, validate "
            "biến thể, đồng bộ session và giỏ hàng database nếu khách đã đăng nhập."
        ),
        lanes=(CUSTOMER, WEB, BACKEND),
        nodes=(
            n("start", "start", CUSTOMER),
            n("select_variant", "action", CUSTOMER, "Chọn biến thể sản phẩm"),
            n("choose_action", "decision", CUSTOMER, "Thao tác mua?"),
            n("check_available", "action", WEB, "Kiểm tra trạng thái tồn kho trên UI"),
            n("available", "decision", WEB, "Còn hàng?"),
            n("show_stock_error", "action", WEB, "Thông báo biến thể hết hàng"),
            n("post_add", "action", WEB, "POST /Cart/AddItem"),
            n("ensure_loaded", "action", BACKEND, "Nếu đã đăng nhập, nạp giỏ đã lưu"),
            n("validate_item", "action", BACKEND, "Validate biến thể còn bán và còn tồn"),
            n("valid_item", "decision", BACKEND, "Biến thể hợp lệ?"),
            n("clear_checkout", "action", BACKEND, "Xóa checkout selection cũ"),
            n("save_cart", "action", BACKEND, "Add/update session, revalidate và persist"),
            n("return_count", "action", WEB, "Trả số lượng giỏ và cập nhật badge"),
            n("post_buy_now", "action", WEB, "POST /Cart/BuyNow"),
            n("save_buy_now", "action", BACKEND, "Validate và lưu BuyNow session"),
            n("buy_valid", "decision", BACKEND, "BuyNow hợp lệ?"),
            n("redirect_checkout", "action", WEB, "Redirect /Checkout?mode=buynow"),
            n("show_error", "action", WEB, "Thông báo lỗi cập nhật giỏ"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "select_variant"),
            e("select_variant", "choose_action"),
            e("choose_action", "check_available", "Thêm giỏ"),
            e("check_available", "available"),
            e("available", "show_stock_error", "Không"),
            e("show_stock_error", "end"),
            e("available", "post_add", "Có"),
            e("post_add", "ensure_loaded"),
            e("ensure_loaded", "validate_item"),
            e("validate_item", "valid_item"),
            e("valid_item", "show_error", "Không"),
            e("show_error", "end"),
            e("valid_item", "clear_checkout", "Có"),
            e("clear_checkout", "save_cart"),
            e("save_cart", "return_count"),
            e("return_count", "end"),
            e("choose_action", "post_buy_now", "Mua ngay"),
            e("post_buy_now", "save_buy_now"),
            e("save_buy_now", "buy_valid"),
            e("buy_valid", "show_error", "Không"),
            e("buy_valid", "redirect_checkout", "Có"),
            e("redirect_checkout", "end"),
        ),
    ),
    FlowSpec(
        key="cart_prepare_checkout",
        name="04 Quản lý giỏ hàng và chuẩn bị checkout",
        documentation=(
            "Luồng trang giỏ hàng: nạp giỏ từ session/database, cập nhật số lượng, "
            "xóa sản phẩm, chọn sản phẩm thanh toán và lưu checkout selection riêng."
        ),
        lanes=(CUSTOMER, WEB, BACKEND),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_cart", "action", CUSTOMER, "Mở trang giỏ hàng"),
            n("load_session", "action", BACKEND, "Đọc cart session"),
            n("need_db_load", "decision", BACKEND, "Trạng thái session / đăng nhập?"),
            n("load_db_cart", "action", BACKEND, "Nạp giỏ đã lưu từ database"),
            n("persist_current", "action", BACKEND, "Đồng bộ session hiện tại vào database"),
            n("build_cart", "action", BACKEND, "Build item và gợi ý phụ kiện"),
            n("render_cart", "action", WEB, "Render giỏ hàng"),
            n("edit_cart", "action", CUSTOMER, "Tăng giảm, xóa, chọn sản phẩm"),
            n("save_session", "action", WEB, "POST /Cart/SaveSession"),
            n("has_items", "decision", BACKEND, "Danh sách còn sản phẩm?"),
            n("clear_cart", "action", BACKEND, "Clear session và giỏ database"),
            n("validate_items", "action", BACKEND, "Validate lại từng sản phẩm"),
            n("has_valid", "decision", BACKEND, "Còn sản phẩm hợp lệ?"),
            n("save_valid", "action", BACKEND, "Lưu session và persist nếu đăng nhập"),
            n("update_summary", "action", WEB, "Cập nhật tổng tiền và badge"),
            n("click_checkout", "action", CUSTOMER, "Bấm thanh toán"),
            n("selected", "decision", WEB, "Đã chọn sản phẩm?"),
            n("collect_checkout", "action", WEB, "Gom sản phẩm chọn và gợi ý đã chọn"),
            n("prepare_checkout", "action", BACKEND, "POST /Cart/PrepareCheckout"),
            n("valid_selected", "decision", BACKEND, "Có sản phẩm hợp lệ?"),
            n("save_selection", "action", BACKEND, "Lưu CheckoutSelection session"),
            n("redirect_checkout", "action", WEB, "Redirect /Checkout?mode=selected"),
            n("show_error", "action", WEB, "Hiển thị lỗi chuẩn bị checkout"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_cart"),
            e("open_cart", "load_session"),
            e("load_session", "need_db_load"),
            e("need_db_load", "load_db_cart", "Rỗng + đăng nhập"),
            e("load_db_cart", "build_cart"),
            e("need_db_load", "persist_current", "Có session + đăng nhập"),
            e("persist_current", "build_cart"),
            e("need_db_load", "build_cart", "Khách vãng lai"),
            e("build_cart", "render_cart"),
            e("render_cart", "edit_cart"),
            e("edit_cart", "save_session"),
            e("save_session", "has_items"),
            e("has_items", "clear_cart", "Không"),
            e("clear_cart", "update_summary"),
            e("has_items", "validate_items", "Có"),
            e("validate_items", "has_valid"),
            e("has_valid", "show_error", "Không"),
            e("has_valid", "save_valid", "Có"),
            e("save_valid", "update_summary"),
            e("update_summary", "click_checkout"),
            e("click_checkout", "selected"),
            e("selected", "show_error", "Không"),
            e("selected", "collect_checkout", "Có"),
            e("collect_checkout", "prepare_checkout"),
            e("prepare_checkout", "valid_selected"),
            e("valid_selected", "show_error", "Không"),
            e("valid_selected", "save_selection", "Có"),
            e("save_selection", "redirect_checkout"),
            e("show_error", "end"),
            e("redirect_checkout", "end"),
        ),
    ),
    FlowSpec(
        key="checkout_cod",
        name="05 Checkout COD và đặt hàng thành công",
        documentation=(
            "Luồng checkout khi khách chọn COD: yêu cầu đăng nhập, dựng checkout "
            "model, validate voucher/form, tạo đơn trong transaction, trừ tồn kho, "
            "xóa đúng phần giỏ đã hoàn tất và hiển thị trang thành công."
        ),
        lanes=(CUSTOMER, WEB, BACKEND, EXTERNAL),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_checkout", "action", CUSTOMER, "Mở trang checkout"),
            n("check_login", "decision", WEB, "Đã đăng nhập?"),
            n("redirect_login", "action", WEB, "Redirect Login kèm returnUrl"),
            n("build_model", "action", BACKEND, "BuildModel theo mode: cart, selected, buynow"),
            n("has_items", "decision", BACKEND, "Có sản phẩm checkout?"),
            n("back_cart", "action", WEB, "Redirect về giỏ hàng"),
            n("load_checkout_data", "action", BACKEND, "Nạp payment methods, voucher, địa chỉ mặc định"),
            n("load_location", "action", EXTERNAL, "API tỉnh / huyện / xã khi nhập địa chỉ"),
            n("render_form", "action", WEB, "Render form checkout"),
            n("fill_form", "action", CUSTOMER, "Nhập giao hàng, chọn COD, chọn voucher"),
            n("change_voucher", "action", BACKEND, "Validate voucher qua /Checkout/ChangeVoucher"),
            n("submit", "action", CUSTOMER, "Bấm đặt hàng"),
            n("post_checkout", "action", WEB, "POST /Checkout"),
            n("model_valid", "decision", BACKEND, "Form và voucher hợp lệ?"),
            n("redisplay_error", "action", WEB, "Render lại checkout kèm lỗi"),
            n("place_order", "action", BACKEND, "PlaceOrder clearCart=true trong transaction"),
            n("order_valid", "decision", BACKEND, "User, địa chỉ, payment, tồn kho hợp lệ?"),
            n("rollback", "action", BACKEND, "Rollback và trả OrderPlacementException"),
            n("commit_order", "action", BACKEND, "Tạo đơn, trừ tồn, ghi voucher usage, commit"),
            n("store_success", "action", WEB, "Lưu success model vào session"),
            n("clear_completed", "action", BACKEND, "Clear BuyNow/CheckoutSelection và refresh giỏ"),
            n("success_page", "action", WEB, "Redirect /Checkout/Success"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_checkout"),
            e("open_checkout", "check_login"),
            e("check_login", "redirect_login", "Không"),
            e("redirect_login", "end"),
            e("check_login", "build_model", "Có"),
            e("build_model", "has_items"),
            e("has_items", "back_cart", "Không"),
            e("back_cart", "end"),
            e("has_items", "load_checkout_data", "Có"),
            e("load_checkout_data", "load_location"),
            e("load_location", "render_form"),
            e("render_form", "fill_form"),
            e("fill_form", "change_voucher", "Đổi voucher"),
            e("change_voucher", "fill_form"),
            e("fill_form", "submit"),
            e("submit", "post_checkout"),
            e("post_checkout", "model_valid"),
            e("model_valid", "redisplay_error", "Không"),
            e("redisplay_error", "end"),
            e("model_valid", "place_order", "Có"),
            e("place_order", "order_valid"),
            e("order_valid", "rollback", "Không"),
            e("rollback", "redisplay_error"),
            e("order_valid", "commit_order", "Có"),
            e("commit_order", "store_success"),
            e("store_success", "clear_completed"),
            e("clear_completed", "success_page"),
            e("success_page", "end"),
        ),
    ),
    FlowSpec(
        key="checkout_online_payment",
        name="06 Checkout thanh toán online MoMo / VNPay",
        documentation=(
            "Luồng thanh toán online sau khi đặt đơn: tạo đơn pending/unpaid, chuyển "
            "sang MoMo hoặc VNPay, xử lý return/IPN, xác nhận thanh toán hoặc hủy đơn "
            "thất bại và hoàn tồn kho."
        ),
        lanes=(CUSTOMER, WEB, BACKEND, EXTERNAL),
        nodes=(
            n("start", "start", CUSTOMER),
            n("submit_online", "action", CUSTOMER, "Submit checkout chọn MoMo / VNPay"),
            n("validate_snapshot", "action", BACKEND, "Validate snapshot và voucher"),
            n("place_order", "action", BACKEND, "PlaceOrder clearCart=false"),
            n("store_success", "action", WEB, "Lưu success model"),
            n("create_payment", "action", BACKEND, "Tạo request thanh toán online"),
            n("url_created", "decision", BACKEND, "Có payUrl?"),
            n("payment_error", "action", WEB, "TempData PaymentError và về OrderDetail"),
            n("redirect_gateway", "action", WEB, "Redirect sang cổng thanh toán"),
            n("pay_on_gateway", "action", EXTERNAL, "Khách thanh toán trên MoMo/VNPay"),
            n("callback", "action", WEB, "Nhận return / IPN từ cổng"),
            n("process_callback", "action", BACKEND, "Kiểm tra chữ ký và mã kết quả"),
            n("payment_success", "decision", BACKEND, "Thanh toán thành công?"),
            n("confirm_payment", "action", BACKEND, "ConfirmOnlinePayment và xóa item đã mua"),
            n("clear_session", "action", WEB, "Clear BuyNow/CheckoutSelection, refresh giỏ"),
            n("show_success", "action", WEB, "Hiển thị kết quả thành công"),
            n("cancel_order", "action", BACKEND, "CancelFailedOrder và hoàn tồn kho"),
            n("show_failed", "action", WEB, "Hiển thị kết quả thất bại"),
            n("ack_ipn", "action", WEB, "ACK IPN cho cổng thanh toán"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "submit_online"),
            e("submit_online", "validate_snapshot"),
            e("validate_snapshot", "place_order"),
            e("place_order", "store_success"),
            e("store_success", "create_payment"),
            e("create_payment", "url_created"),
            e("url_created", "payment_error", "Không"),
            e("payment_error", "end"),
            e("url_created", "redirect_gateway", "Có"),
            e("redirect_gateway", "pay_on_gateway"),
            e("pay_on_gateway", "callback"),
            e("callback", "process_callback"),
            e("process_callback", "payment_success"),
            e("payment_success", "confirm_payment", "Có"),
            e("confirm_payment", "clear_session"),
            e("clear_session", "show_success"),
            e("show_success", "end"),
            e("payment_success", "cancel_order", "Không"),
            e("cancel_order", "show_failed"),
            e("show_failed", "end"),
            e("process_callback", "ack_ipn", "IPN nền"),
            e("ack_ipn", "end"),
        ),
    ),
    FlowSpec(
        key="sepay_payment_webhook",
        name="07 Thanh toán SePay chuyển khoản và webhook",
        documentation=(
            "Luồng SePay: checkout chuyển sang trang QR/chuyển khoản, khách chờ "
            "poll trạng thái, webhook SePay xác thực giao dịch, đối soát order code, "
            "số tài khoản, số tiền và xác nhận thanh toán."
        ),
        lanes=(CUSTOMER, WEB, BACKEND, EXTERNAL),
        nodes=(
            n("start", "start", CUSTOMER),
            n("select_sepay", "action", CUSTOMER, "Submit checkout chọn SePay"),
            n("place_order", "action", BACKEND, "Tạo đơn pending/unpaid clearCart=false"),
            n("redirect_sepay", "action", WEB, "Redirect /payment/sepay/{orderCode}"),
            n("check_login", "decision", WEB, "Phiên đăng nhập còn hợp lệ?"),
            n("login", "action", WEB, "Redirect Login"),
            n("load_payment", "action", BACKEND, "GetPaymentAsync: kiểm tra đơn thuộc khách"),
            n("payment_found", "decision", BACKEND, "Tìm thấy đơn SePay?"),
            n("not_found", "action", WEB, "Trả 404"),
            n("render_qr", "action", WEB, "Render QR, tài khoản, nội dung chuyển khoản"),
            n("copy_transfer", "action", CUSTOMER, "Sao chép thông tin và chuyển khoản"),
            n("poll_status", "action", WEB, "Poll /api/payments/sepay/{code}/status"),
            n("bank_webhook", "action", EXTERNAL, "SePay gửi webhook giao dịch"),
            n("auth_webhook", "action", BACKEND, "Xác thực header và raw body"),
            n("webhook_valid", "decision", BACKEND, "Webhook hợp lệ?"),
            n("reject_webhook", "action", WEB, "Trả Unauthorized / BadRequest"),
            n("parse_payload", "action", BACKEND, "Parse payload và chống trùng transaction"),
            n("match_order", "action", BACKEND, "Đối soát inbound, order code, account, amount"),
            n("matched", "decision", BACKEND, "Đủ điều kiện xác nhận?"),
            n("record_unmatched", "action", BACKEND, "Lưu event trạng thái ignored/rejected"),
            n("confirm_paid", "action", BACKEND, "ConfirmOnlinePayment cho đơn SePay"),
            n("return_paid", "action", WEB, "Status trả paid"),
            n("show_paid", "action", WEB, "Hiện overlay thành công và về Success"),
            n("cancel", "action", CUSTOMER, "Khách hủy thanh toán"),
            n("cancel_order", "action", BACKEND, "CancelFailedOrder và về giỏ hàng"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "select_sepay"),
            e("select_sepay", "place_order"),
            e("place_order", "redirect_sepay"),
            e("redirect_sepay", "check_login"),
            e("check_login", "login", "Không"),
            e("login", "end"),
            e("check_login", "load_payment", "Có"),
            e("load_payment", "payment_found"),
            e("payment_found", "not_found", "Không"),
            e("not_found", "end"),
            e("payment_found", "render_qr", "Có"),
            e("render_qr", "copy_transfer"),
            e("copy_transfer", "poll_status"),
            e("copy_transfer", "bank_webhook", "Ngân hàng ghi nhận"),
            e("bank_webhook", "auth_webhook"),
            e("auth_webhook", "webhook_valid"),
            e("webhook_valid", "reject_webhook", "Không"),
            e("reject_webhook", "end"),
            e("webhook_valid", "parse_payload", "Có"),
            e("parse_payload", "match_order"),
            e("match_order", "matched"),
            e("matched", "record_unmatched", "Không"),
            e("record_unmatched", "end"),
            e("matched", "confirm_paid", "Có"),
            e("confirm_paid", "return_paid"),
            e("poll_status", "return_paid", "Khi paid"),
            e("return_paid", "show_paid"),
            e("show_paid", "end"),
            e("render_qr", "cancel", "Khách hủy"),
            e("cancel", "cancel_order"),
            e("cancel_order", "end"),
        ),
    ),
    FlowSpec(
        key="profile_address",
        name="08 Hồ sơ cá nhân và sổ địa chỉ",
        documentation=(
            "Luồng trang hồ sơ: kiểm tra session, nạp tổng quan tài khoản, lịch sử "
            "đơn, địa chỉ, yêu thích; thêm địa chỉ và đặt địa chỉ mặc định."
        ),
        lanes=(CUSTOMER, WEB, BACKEND),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_profile", "action", CUSTOMER, "Mở /Account/Profile"),
            n("check_login", "decision", WEB, "Đã đăng nhập?"),
            n("redirect_login", "action", WEB, "Redirect Login kèm returnUrl"),
            n("load_profile", "action", BACKEND, "Nạp summary, orders, addresses, favorites"),
            n("render_profile", "action", WEB, "Render tab hồ sơ"),
            n("choose_address_action", "decision", CUSTOMER, "Quản lý địa chỉ?"),
            n("enter_address", "action", CUSTOMER, "Nhập địa chỉ giao hàng"),
            n("post_address", "action", WEB, "POST AddAddress"),
            n("address_model_valid", "decision", BACKEND, "Form địa chỉ hợp lệ?"),
            n("resolve_user", "action", BACKEND, "Resolve user đang hoạt động"),
            n("address_valid", "decision", BACKEND, "Dữ liệu bắt buộc đầy đủ?"),
            n("save_address", "action", BACKEND, "Transaction: thêm địa chỉ, xử lý mặc định"),
            n("set_default", "action", CUSTOMER, "Chọn đặt làm mặc định"),
            n("post_default", "action", WEB, "POST SetDefaultAddress"),
            n("update_default", "action", BACKEND, "Bỏ default cũ và đặt default mới"),
            n("flash_result", "action", WEB, "TempData success/error và redirect tab info"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_profile"),
            e("open_profile", "check_login"),
            e("check_login", "redirect_login", "Không"),
            e("redirect_login", "end"),
            e("check_login", "load_profile", "Có"),
            e("load_profile", "render_profile"),
            e("render_profile", "choose_address_action"),
            e("choose_address_action", "enter_address", "Thêm địa chỉ"),
            e("enter_address", "post_address"),
            e("post_address", "address_model_valid"),
            e("address_model_valid", "flash_result", "Không"),
            e("address_model_valid", "resolve_user", "Có"),
            e("resolve_user", "address_valid"),
            e("address_valid", "flash_result", "Không"),
            e("address_valid", "save_address", "Có"),
            e("save_address", "flash_result"),
            e("choose_address_action", "set_default", "Đặt mặc định"),
            e("set_default", "post_default"),
            e("post_default", "update_default"),
            e("update_default", "flash_result"),
            e("choose_address_action", "end", "Chỉ xem"),
            e("flash_result", "end"),
        ),
    ),
    FlowSpec(
        key="order_review",
        name="09 Chi tiết đơn hàng, vận chuyển và đánh giá sau mua",
        documentation=(
            "Luồng xem chi tiết đơn hàng và gửi đánh giá: chỉ khách sở hữu đơn, đơn "
            "đã thanh toán và hoàn tất mới được đánh giá đúng order item/variant."
        ),
        lanes=(CUSTOMER, WEB, BACKEND),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_order", "action", CUSTOMER, "Mở /account/orders/{code}"),
            n("check_login", "decision", WEB, "Đã đăng nhập?"),
            n("redirect_login", "action", WEB, "Redirect Login kèm returnUrl"),
            n("load_order", "action", BACKEND, "Nạp order detail, items, payment, shipment"),
            n("order_found", "decision", BACKEND, "Đơn thuộc khách và tồn tại?"),
            n("not_found", "action", WEB, "Trả 404"),
            n("render_order", "action", WEB, "Render tổng quan, stepper, shipment, review form"),
            n("can_review", "decision", WEB, "Có sản phẩm được đánh giá?"),
            n("submit_review", "action", CUSTOMER, "Chọn sao, nhập bình luận, gửi review"),
            n("post_review", "action", WEB, "POST /account/orders/reviews"),
            n("model_valid", "decision", BACKEND, "Form review hợp lệ?"),
            n("validate_review", "action", BACKEND, "Kiểm tra owner, paid, completed, comment length"),
            n("review_allowed", "decision", BACKEND, "Được phép lưu?"),
            n("save_review", "action", BACKEND, "Tạo/cập nhật Rating theo OrderItem"),
            n("refresh_rating", "action", BACKEND, "Cập nhật rating summary sản phẩm"),
            n("redirect_anchor", "action", WEB, "Redirect #order-reviews kèm thông báo"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_order"),
            e("open_order", "check_login"),
            e("check_login", "redirect_login", "Không"),
            e("redirect_login", "end"),
            e("check_login", "load_order", "Có"),
            e("load_order", "order_found"),
            e("order_found", "not_found", "Không"),
            e("not_found", "end"),
            e("order_found", "render_order", "Có"),
            e("render_order", "can_review"),
            e("can_review", "end", "Không"),
            e("can_review", "submit_review", "Có"),
            e("submit_review", "post_review"),
            e("post_review", "model_valid"),
            e("model_valid", "redirect_anchor", "Không"),
            e("model_valid", "validate_review", "Có"),
            e("validate_review", "review_allowed"),
            e("review_allowed", "redirect_anchor", "Không"),
            e("review_allowed", "save_review", "Có"),
            e("save_review", "refresh_rating"),
            e("refresh_rating", "redirect_anchor"),
            e("redirect_anchor", "end"),
        ),
    ),
    FlowSpec(
        key="wishlist",
        name="10 Yêu thích sản phẩm",
        documentation=(
            "Luồng wishlist trên product card/product detail/profile: kiểm tra đăng "
            "nhập, lấy trạng thái, toggle hoặc remove sản phẩm yêu thích."
        ),
        lanes=(CUSTOMER, WEB, BACKEND),
        nodes=(
            n("start", "start", CUSTOMER),
            n("load_cards", "action", WEB, "Product card yêu cầu /wishlist/status"),
            n("status_login", "decision", BACKEND, "Đã đăng nhập?"),
            n("status_result", "action", WEB, "Cập nhật trạng thái yêu thích trên UI"),
            n("click_toggle", "action", CUSTOMER, "Bấm thêm / bỏ yêu thích"),
            n("post_toggle", "action", WEB, "POST /wishlist/toggle"),
            n("toggle_login", "decision", BACKEND, "Đã đăng nhập?"),
            n("redirect_login", "action", WEB, "Redirect Login từ loginUrl"),
            n("request_valid", "decision", BACKEND, "ProductId hợp lệ?"),
            n("resolve_variant", "action", BACKEND, "Resolve user và biến thể còn bán"),
            n("target_found", "decision", BACKEND, "Tìm thấy user và variant?"),
            n("exists", "decision", BACKEND, "Đã có trong wishlist?"),
            n("remove", "action", BACKEND, "Xóa khỏi wishlist"),
            n("add", "action", BACKEND, "Thêm vào wishlist"),
            n("json_update", "action", WEB, "Trả JSON và cập nhật icon/message"),
            n("profile_remove", "action", CUSTOMER, "Xóa yêu thích từ tab hồ sơ"),
            n("post_remove", "action", WEB, "POST /wishlist/remove"),
            n("dom_remove", "action", WEB, "Xóa card khỏi danh sách hồ sơ"),
            n("show_error", "action", WEB, "Hiển thị lỗi"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "load_cards"),
            e("load_cards", "status_login"),
            e("status_login", "status_result", "Có"),
            e("status_login", "click_toggle", "Không"),
            e("status_result", "click_toggle"),
            e("click_toggle", "post_toggle"),
            e("post_toggle", "toggle_login"),
            e("toggle_login", "redirect_login", "Không"),
            e("redirect_login", "end"),
            e("toggle_login", "request_valid", "Có"),
            e("request_valid", "show_error", "Không"),
            e("request_valid", "resolve_variant", "Có"),
            e("resolve_variant", "target_found"),
            e("target_found", "show_error", "Không"),
            e("target_found", "exists", "Có"),
            e("exists", "remove", "Có"),
            e("exists", "add", "Không"),
            e("remove", "json_update"),
            e("add", "json_update"),
            e("json_update", "profile_remove", "Nếu ở hồ sơ"),
            e("profile_remove", "post_remove"),
            e("post_remove", "dom_remove"),
            e("dom_remove", "end"),
            e("json_update", "end"),
            e("show_error", "end"),
        ),
    ),
    FlowSpec(
        key="support_chat",
        name="11 Chat hỗ trợ với admin",
        documentation=(
            "Luồng chat hỗ trợ realtime: bootstrap hội thoại, yêu cầu đăng nhập, "
            "kết nối SignalR nếu có token, gửi tin qua realtime hoặc fallback HTTP."
        ),
        lanes=(CUSTOMER, WEB, BACKEND, ADMIN),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_chat", "action", CUSTOMER, "Mở chat hỗ trợ"),
            n("bootstrap", "action", WEB, "GET /api/customer-messages/bootstrap?channel=support"),
            n("load_bootstrap", "action", BACKEND, "Resolve user, conversation, messages, token"),
            n("logged_in", "decision", BACKEND, "Khách đã đăng nhập?"),
            n("login_prompt", "action", WEB, "Hiện yêu cầu đăng nhập"),
            n("render_messages", "action", WEB, "Render lịch sử hội thoại"),
            n("connect_realtime", "action", WEB, "Kết nối SignalR bằng access token"),
            n("connected", "decision", WEB, "Realtime sẵn sàng?"),
            n("fallback_notice", "action", WEB, "Báo dùng kết nối dự phòng"),
            n("send_message", "action", CUSTOMER, "Nhập và gửi tin nhắn"),
            n("append_pending", "action", WEB, "Hiển thị tin đang gửi"),
            n("send_signalr", "action", WEB, "Invoke SendCustomerMessage"),
            n("signalr_ok", "decision", ADMIN, "Admin API nhận realtime?"),
            n("http_fallback", "action", WEB, "POST support-messages bằng token"),
            n("persist_message", "action", ADMIN, "Lưu tin nhắn và trả conversationId"),
            n("join_conversation", "action", WEB, "Join conversation hiện tại"),
            n("admin_reply", "action", ADMIN, "Admin phản hồi"),
            n("receive_reply", "action", WEB, "Nhận MessageReceived và cập nhật chat"),
            n("send_error", "action", WEB, "Đánh dấu gửi thất bại"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_chat"),
            e("open_chat", "bootstrap"),
            e("bootstrap", "load_bootstrap"),
            e("load_bootstrap", "logged_in"),
            e("logged_in", "login_prompt", "Không"),
            e("login_prompt", "end"),
            e("logged_in", "render_messages", "Có"),
            e("render_messages", "connect_realtime"),
            e("connect_realtime", "connected"),
            e("connected", "fallback_notice", "Không"),
            e("fallback_notice", "send_message"),
            e("connected", "send_message", "Có"),
            e("send_message", "append_pending"),
            e("append_pending", "send_signalr"),
            e("send_signalr", "signalr_ok"),
            e("signalr_ok", "http_fallback", "Không"),
            e("http_fallback", "persist_message"),
            e("signalr_ok", "persist_message", "Có"),
            e("persist_message", "join_conversation"),
            e("join_conversation", "admin_reply"),
            e("admin_reply", "receive_reply"),
            e("receive_reply", "end"),
            e("http_fallback", "send_error", "Lỗi"),
            e("send_error", "end"),
        ),
    ),
    FlowSpec(
        key="ai_assistant",
        name="12 Trợ lý AI tư vấn sản phẩm",
        documentation=(
            "Luồng AI chatbox: bootstrap kênh AI, gửi câu hỏi tới /api/chat, backend "
            "tìm sản phẩm liên quan, gọi Gemini, trả lời kèm gợi ý sản phẩm và lưu "
            "lịch sử nếu khách đã đăng nhập."
        ),
        lanes=(CUSTOMER, WEB, BACKEND, EXTERNAL),
        nodes=(
            n("start", "start", CUSTOMER),
            n("open_ai", "action", CUSTOMER, "Mở trợ lý AI"),
            n("bootstrap_ai", "action", WEB, "Bootstrap channel=ai"),
            n("load_ai_history", "action", BACKEND, "Nạp hội thoại AI nếu đã đăng nhập"),
            n("connect_optional", "action", WEB, "Kết nối realtime nếu có token"),
            n("ask_question", "action", CUSTOMER, "Nhập câu hỏi tư vấn"),
            n("post_chat", "action", WEB, "POST /api/chat"),
            n("message_valid", "decision", BACKEND, "Câu hỏi hợp lệ?"),
            n("bad_request", "action", WEB, "Trả lỗi nhập câu hỏi"),
            n("find_products", "action", BACKEND, "Tìm sản phẩm liên quan từ database"),
            n("build_context", "action", BACKEND, "Build context sản phẩm và lịch sử"),
            n("call_gemini", "action", EXTERNAL, "Gọi Gemini"),
            n("gemini_ok", "decision", EXTERNAL, "Gemini trả lời?"),
            n("service_error", "action", WEB, "Hiển thị lỗi 503/500"),
            n("parse_reply", "action", BACKEND, "Parse productIds và chuẩn hóa reply"),
            n("create_receipt", "action", BACKEND, "Tạo receipt lưu lịch sử nếu có user"),
            n("render_reply", "action", WEB, "Hiển thị trả lời và product cards"),
            n("save_history", "decision", WEB, "Có đăng nhập và receipt?"),
            n("persist_ai", "action", WEB, "Lưu AI exchange qua SignalR hoặc HTTP"),
            n("login_note", "action", WEB, "Nhắc đăng nhập để lưu lịch sử"),
            n("open_product", "action", CUSTOMER, "Mở sản phẩm AI gợi ý"),
            n("end", "final", WEB),
        ),
        edges=(
            e("start", "open_ai"),
            e("open_ai", "bootstrap_ai"),
            e("bootstrap_ai", "load_ai_history"),
            e("load_ai_history", "connect_optional"),
            e("connect_optional", "ask_question"),
            e("ask_question", "post_chat"),
            e("post_chat", "message_valid"),
            e("message_valid", "bad_request", "Không"),
            e("bad_request", "end"),
            e("message_valid", "find_products", "Có"),
            e("find_products", "build_context"),
            e("build_context", "call_gemini"),
            e("call_gemini", "gemini_ok"),
            e("gemini_ok", "service_error", "Không"),
            e("service_error", "end"),
            e("gemini_ok", "parse_reply", "Có"),
            e("parse_reply", "create_receipt"),
            e("create_receipt", "render_reply"),
            e("render_reply", "save_history"),
            e("save_history", "persist_ai", "Có"),
            e("persist_ai", "open_product"),
            e("save_history", "login_note", "Không"),
            e("login_note", "open_product"),
            e("open_product", "end"),
        ),
    ),
)


NATURAL_FLOW_NAMES = {
    "auth_firebase_sync": "01 Đăng nhập và đăng ký tài khoản",
    "catalog_search_product": "02 Tìm và xem sản phẩm",
    "product_cart_buy_now": "03 Chọn sản phẩm để mua",
    "cart_prepare_checkout": "04 Quản lý giỏ hàng và chọn món thanh toán",
    "checkout_cod": "05 Đặt hàng thanh toán khi nhận hàng",
    "checkout_online_payment": "06 Đặt hàng qua ví hoặc ngân hàng",
    "sepay_payment_webhook": "07 Chuyển khoản SePay và xác nhận giao dịch",
    "profile_address": "08 Xem hồ sơ và quản lý địa chỉ",
    "order_review": "09 Theo dõi đơn hàng và gửi đánh giá",
    "wishlist": "10 Lưu sản phẩm yêu thích",
    "support_chat": "11 Nhắn tin với nhân viên hỗ trợ",
    "ai_assistant": "12 Hỏi trợ lý tư vấn sản phẩm",
}


NATURAL_DOCUMENTATION = {
    "auth_firebase_sync": (
        "Khách mở trang đăng nhập hoặc đăng ký, chọn cách xác thực, hệ thống kiểm tra "
        "thông tin và ghi nhớ trạng thái đăng nhập để khách quay lại trang mong muốn."
    ),
    "catalog_search_product": (
        "Khách tìm sản phẩm từ trang chủ, menu danh mục hoặc ô tìm kiếm, xem danh sách "
        "phù hợp rồi mở trang chi tiết để chọn màu, phiên bản và xem tình trạng còn hàng."
    ),
    "product_cart_buy_now": (
        "Khách chọn phiên bản sản phẩm, thêm vào giỏ hoặc mua ngay; hệ thống kiểm tra "
        "sản phẩm còn bán, còn hàng rồi cập nhật giỏ hoặc chuyển sang bước đặt hàng."
    ),
    "cart_prepare_checkout": (
        "Khách mở giỏ hàng, chỉnh số lượng, xóa món, chọn món muốn thanh toán; hệ thống "
        "chỉ ghi nhớ các món đã chọn để đặt hàng, không làm mất những món chưa chọn."
    ),
    "checkout_cod": (
        "Khách nhập thông tin nhận hàng, chọn thanh toán khi nhận hàng, áp dụng ưu đãi "
        "nếu có; hệ thống kiểm tra đơn, lưu đơn và hiển thị trang đặt hàng thành công."
    ),
    "checkout_online_payment": (
        "Khách chọn thanh toán qua ví hoặc ngân hàng, được chuyển sang cổng thanh toán; "
        "sau khi có kết quả, hệ thống xác nhận đơn thành công hoặc hủy đơn thất bại."
    ),
    "sepay_payment_webhook": (
        "Khách chọn chuyển khoản SePay, xem mã QR và nội dung chuyển khoản; hệ thống tự "
        "đối chiếu giao dịch từ ngân hàng rồi cập nhật trạng thái đơn hàng."
    ),
    "profile_address": (
        "Khách vào hồ sơ để xem thông tin cá nhân, đơn hàng, địa chỉ và sản phẩm yêu "
        "thích; khách có thể thêm địa chỉ mới hoặc đặt địa chỉ mặc định."
    ),
    "order_review": (
        "Khách mở chi tiết đơn để theo dõi trạng thái, vận chuyển và chỉ gửi đánh giá "
        "khi đơn đã hoàn tất, đã thanh toán và đúng sản phẩm đã mua."
    ),
    "wishlist": (
        "Khách bấm yêu thích trên sản phẩm hoặc xóa sản phẩm khỏi danh sách yêu thích; "
        "nếu chưa đăng nhập, hệ thống mời khách đăng nhập trước."
    ),
    "support_chat": (
        "Khách mở khung hỗ trợ, đăng nhập nếu cần, gửi tin nhắn cho nhân viên và nhận "
        "phản hồi ngay trên màn hình trò chuyện."
    ),
    "ai_assistant": (
        "Khách hỏi trợ lý tư vấn sản phẩm; hệ thống tìm sản phẩm phù hợp, tạo câu trả "
        "lời dễ hiểu và lưu lịch sử tư vấn khi khách đã đăng nhập."
    ),
}


NATURAL_NODE_NAMES = {
    ("auth_firebase_sync", "open_login"): "Mở trang đăng nhập hoặc đăng ký",
    ("auth_firebase_sync", "choose_method"): "Chọn cách đăng nhập",
    ("auth_firebase_sync", "firebase_auth"): "Dịch vụ đăng nhập kiểm tra thông tin",
    ("auth_firebase_sync", "has_token"): "Đăng nhập thành công?",
    ("auth_firebase_sync", "auth_error"): "Hiện lỗi để khách thử lại",
    ("auth_firebase_sync", "sync_backend"): "Gửi kết quả đăng nhập về hệ thống",
    ("auth_firebase_sync", "verify_token"): "Hệ thống kiểm tra kết quả đăng nhập",
    ("auth_firebase_sync", "valid_token"): "Kết quả hợp lệ?",
    ("auth_firebase_sync", "resolve_identity"): "Xác định tài khoản khách hàng",
    ("auth_firebase_sync", "profile_exists"): "Đã có hồ sơ khách?",
    ("auth_firebase_sync", "create_profile"): "Tạo hồ sơ khách hàng mới",
    ("auth_firebase_sync", "load_profile"): "Lấy thông tin hồ sơ",
    ("auth_firebase_sync", "save_session"): "Ghi nhớ trạng thái đăng nhập",
    ("auth_firebase_sync", "redirect"): "Chuyển đến trang khách muốn vào",

    ("catalog_search_product", "open_entry"): "Mở trang chủ, danh mục hoặc tìm kiếm",
    ("catalog_search_product", "type_query"): "Nhập từ khóa hoặc chọn danh mục",
    ("catalog_search_product", "suggest"): "Hiện gợi ý khi khách đang gõ",
    ("catalog_search_product", "read_suggestion"): "Tìm sản phẩm gợi ý phù hợp",
    ("catalog_search_product", "submit_navigation"): "Chọn kết quả muốn xem",
    ("catalog_search_product", "send_request"): "Yêu cầu mở trang danh sách",
    ("catalog_search_product", "normalize_query"): "Sắp xếp lại điều kiện tìm kiếm",
    ("catalog_search_product", "build_listing"): "Chuẩn bị danh sách sản phẩm",
    ("catalog_search_product", "listing_found"): "Có trang phù hợp?",
    ("catalog_search_product", "not_found"): "Hiện trang không tìm thấy",
    ("catalog_search_product", "render_listing"): "Hiện danh sách sản phẩm",
    ("catalog_search_product", "sectioned"): "Danh mục có nhiều nhóm?",
    ("catalog_search_product", "load_section"): "Tải lại nhóm sản phẩm được chọn",
    ("catalog_search_product", "choose_product"): "Chọn một sản phẩm",
    ("catalog_search_product", "request_detail"): "Mở trang chi tiết sản phẩm",
    ("catalog_search_product", "load_detail"): "Chuẩn bị thông tin sản phẩm",
    ("catalog_search_product", "product_found"): "Tìm thấy sản phẩm?",
    ("catalog_search_product", "product_404"): "Hiện trang sản phẩm không tồn tại",
    ("catalog_search_product", "render_detail"): "Hiện chi tiết sản phẩm",
    ("catalog_search_product", "select_variant"): "Chọn màu hoặc phiên bản",
    ("catalog_search_product", "sync_variant_ui"): "Cập nhật ảnh, giá và tình trạng hàng",

    ("product_cart_buy_now", "select_variant"): "Chọn màu hoặc phiên bản",
    ("product_cart_buy_now", "choose_action"): "Khách muốn làm gì?",
    ("product_cart_buy_now", "check_available"): "Kiểm tra lựa chọn còn hàng",
    ("product_cart_buy_now", "available"): "Còn hàng?",
    ("product_cart_buy_now", "show_stock_error"): "Báo sản phẩm đã hết hàng",
    ("product_cart_buy_now", "post_add"): "Gửi yêu cầu thêm vào giỏ",
    ("product_cart_buy_now", "ensure_loaded"): "Lấy giỏ hiện có của khách",
    ("product_cart_buy_now", "validate_item"): "Kiểm tra sản phẩm có thể mua",
    ("product_cart_buy_now", "valid_item"): "Có thể thêm vào giỏ?",
    ("product_cart_buy_now", "clear_checkout"): "Bỏ lựa chọn thanh toán cũ",
    ("product_cart_buy_now", "save_cart"): "Lưu sản phẩm vào giỏ",
    ("product_cart_buy_now", "return_count"): "Cập nhật số lượng giỏ hàng",
    ("product_cart_buy_now", "post_buy_now"): "Gửi yêu cầu mua ngay",
    ("product_cart_buy_now", "save_buy_now"): "Ghi nhớ sản phẩm mua ngay",
    ("product_cart_buy_now", "buy_valid"): "Có thể mua ngay?",
    ("product_cart_buy_now", "redirect_checkout"): "Chuyển sang bước đặt hàng",
    ("product_cart_buy_now", "show_error"): "Báo khách thử lại",

    ("cart_prepare_checkout", "open_cart"): "Mở giỏ hàng",
    ("cart_prepare_checkout", "load_session"): "Lấy giỏ hàng đang có",
    ("cart_prepare_checkout", "need_db_load"): "Giỏ đang ở trạng thái nào?",
    ("cart_prepare_checkout", "load_db_cart"): "Lấy giỏ đã lưu của tài khoản",
    ("cart_prepare_checkout", "persist_current"): "Lưu lại giỏ hiện tại",
    ("cart_prepare_checkout", "build_cart"): "Chuẩn bị giỏ và gợi ý mua kèm",
    ("cart_prepare_checkout", "render_cart"): "Hiện trang giỏ hàng",
    ("cart_prepare_checkout", "edit_cart"): "Chọn, tăng giảm hoặc xóa sản phẩm",
    ("cart_prepare_checkout", "save_session"): "Lưu thay đổi giỏ hàng",
    ("cart_prepare_checkout", "has_items"): "Giỏ còn sản phẩm?",
    ("cart_prepare_checkout", "clear_cart"): "Làm trống giỏ hàng",
    ("cart_prepare_checkout", "validate_items"): "Kiểm tra lại các sản phẩm",
    ("cart_prepare_checkout", "has_valid"): "Còn sản phẩm mua được?",
    ("cart_prepare_checkout", "save_valid"): "Lưu giỏ sau khi kiểm tra",
    ("cart_prepare_checkout", "update_summary"): "Cập nhật tổng tiền",
    ("cart_prepare_checkout", "click_checkout"): "Bấm nút thanh toán",
    ("cart_prepare_checkout", "selected"): "Đã chọn món để mua?",
    ("cart_prepare_checkout", "collect_checkout"): "Gom các món sẽ thanh toán",
    ("cart_prepare_checkout", "prepare_checkout"): "Chuẩn bị danh sách đặt hàng",
    ("cart_prepare_checkout", "valid_selected"): "Có món hợp lệ?",
    ("cart_prepare_checkout", "save_selection"): "Ghi nhớ các món sẽ thanh toán",
    ("cart_prepare_checkout", "redirect_checkout"): "Chuyển sang trang đặt hàng",
    ("cart_prepare_checkout", "show_error"): "Báo lỗi để khách chỉnh lại",

    ("checkout_cod", "open_checkout"): "Mở trang đặt hàng",
    ("checkout_cod", "check_login"): "Khách đã đăng nhập?",
    ("checkout_cod", "redirect_login"): "Yêu cầu đăng nhập trước",
    ("checkout_cod", "build_model"): "Chuẩn bị thông tin đơn hàng",
    ("checkout_cod", "has_items"): "Có sản phẩm để đặt?",
    ("checkout_cod", "back_cart"): "Quay về giỏ hàng",
    ("checkout_cod", "load_checkout_data"): "Lấy cách thanh toán, ưu đãi và địa chỉ",
    ("checkout_cod", "load_location"): "Tải danh sách tỉnh, huyện, xã",
    ("checkout_cod", "render_form"): "Hiện form giao hàng và thanh toán",
    ("checkout_cod", "fill_form"): "Nhập thông tin giao hàng",
    ("checkout_cod", "change_voucher"): "Chọn hoặc đổi mã giảm giá",
    ("checkout_cod", "submit"): "Bấm đặt hàng",
    ("checkout_cod", "post_checkout"): "Gửi đơn đặt hàng",
    ("checkout_cod", "model_valid"): "Thông tin đã đầy đủ?",
    ("checkout_cod", "redisplay_error"): "Hiện lỗi để khách sửa",
    ("checkout_cod", "place_order"): "Tạo đơn hàng",
    ("checkout_cod", "order_valid"): "Đơn hàng hợp lệ?",
    ("checkout_cod", "rollback"): "Không lưu đơn và báo lỗi",
    ("checkout_cod", "commit_order"): "Lưu đơn và trừ tồn kho",
    ("checkout_cod", "store_success"): "Ghi nhớ thông tin đơn vừa đặt",
    ("checkout_cod", "clear_completed"): "Cập nhật lại giỏ sau khi đặt",
    ("checkout_cod", "success_page"): "Hiện trang đặt hàng thành công",

    ("checkout_online_payment", "submit_online"): "Đặt hàng với ví hoặc ngân hàng",
    ("checkout_online_payment", "validate_snapshot"): "Kiểm tra lại thông tin đơn",
    ("checkout_online_payment", "place_order"): "Tạo đơn chờ thanh toán",
    ("checkout_online_payment", "store_success"): "Ghi nhớ thông tin đơn",
    ("checkout_online_payment", "create_payment"): "Chuẩn bị đường dẫn thanh toán",
    ("checkout_online_payment", "url_created"): "Mở được cổng thanh toán?",
    ("checkout_online_payment", "payment_error"): "Báo không thể thanh toán",
    ("checkout_online_payment", "redirect_gateway"): "Chuyển sang cổng thanh toán",
    ("checkout_online_payment", "pay_on_gateway"): "Khách thanh toán bên ngoài",
    ("checkout_online_payment", "callback"): "Nhận kết quả thanh toán",
    ("checkout_online_payment", "process_callback"): "Kiểm tra kết quả giao dịch",
    ("checkout_online_payment", "payment_success"): "Thanh toán thành công?",
    ("checkout_online_payment", "confirm_payment"): "Xác nhận đơn đã thanh toán",
    ("checkout_online_payment", "clear_session"): "Cập nhật lại giỏ hàng",
    ("checkout_online_payment", "show_success"): "Hiện kết quả thành công",
    ("checkout_online_payment", "cancel_order"): "Hủy đơn và hoàn lại tồn kho",
    ("checkout_online_payment", "show_failed"): "Hiện kết quả thất bại",
    ("checkout_online_payment", "ack_ipn"): "Ghi nhận thông báo tự động",

    ("sepay_payment_webhook", "select_sepay"): "Chọn chuyển khoản SePay",
    ("sepay_payment_webhook", "place_order"): "Tạo đơn chờ chuyển khoản",
    ("sepay_payment_webhook", "redirect_sepay"): "Mở trang hướng dẫn chuyển khoản",
    ("sepay_payment_webhook", "check_login"): "Khách còn đăng nhập?",
    ("sepay_payment_webhook", "login"): "Yêu cầu đăng nhập lại",
    ("sepay_payment_webhook", "load_payment"): "Lấy thông tin chuyển khoản của đơn",
    ("sepay_payment_webhook", "payment_found"): "Tìm thấy đơn cần thanh toán?",
    ("sepay_payment_webhook", "not_found"): "Hiện trang không tìm thấy",
    ("sepay_payment_webhook", "render_qr"): "Hiện mã QR và nội dung chuyển khoản",
    ("sepay_payment_webhook", "copy_transfer"): "Sao chép thông tin và chuyển khoản",
    ("sepay_payment_webhook", "poll_status"): "Kiểm tra trạng thái thanh toán",
    ("sepay_payment_webhook", "bank_webhook"): "Ngân hàng báo có giao dịch",
    ("sepay_payment_webhook", "auth_webhook"): "Kiểm tra thông báo từ ngân hàng",
    ("sepay_payment_webhook", "webhook_valid"): "Thông báo đáng tin cậy?",
    ("sepay_payment_webhook", "reject_webhook"): "Từ chối thông báo không hợp lệ",
    ("sepay_payment_webhook", "parse_payload"): "Đọc thông tin giao dịch",
    ("sepay_payment_webhook", "match_order"): "So khớp đơn, tài khoản và số tiền",
    ("sepay_payment_webhook", "matched"): "Giao dịch khớp đơn hàng?",
    ("sepay_payment_webhook", "record_unmatched"): "Lưu giao dịch chưa khớp",
    ("sepay_payment_webhook", "confirm_paid"): "Xác nhận đơn đã thanh toán",
    ("sepay_payment_webhook", "return_paid"): "Báo trạng thái đã thanh toán",
    ("sepay_payment_webhook", "show_paid"): "Hiện thành công và về trang kết quả",
    ("sepay_payment_webhook", "cancel"): "Khách hủy chờ chuyển khoản",
    ("sepay_payment_webhook", "cancel_order"): "Hủy đơn và quay về giỏ",

    ("profile_address", "open_profile"): "Mở trang hồ sơ",
    ("profile_address", "check_login"): "Khách đã đăng nhập?",
    ("profile_address", "redirect_login"): "Yêu cầu đăng nhập trước",
    ("profile_address", "load_profile"): "Lấy hồ sơ, đơn hàng, địa chỉ, yêu thích",
    ("profile_address", "render_profile"): "Hiện trang hồ sơ",
    ("profile_address", "choose_address_action"): "Khách thao tác địa chỉ?",
    ("profile_address", "enter_address"): "Nhập địa chỉ nhận hàng",
    ("profile_address", "post_address"): "Gửi yêu cầu thêm địa chỉ",
    ("profile_address", "address_model_valid"): "Thông tin nhập đúng?",
    ("profile_address", "resolve_user"): "Tìm tài khoản khách hàng",
    ("profile_address", "address_valid"): "Địa chỉ đầy đủ?",
    ("profile_address", "save_address"): "Lưu địa chỉ mới",
    ("profile_address", "set_default"): "Chọn làm địa chỉ mặc định",
    ("profile_address", "post_default"): "Gửi yêu cầu đặt mặc định",
    ("profile_address", "update_default"): "Cập nhật địa chỉ mặc định",
    ("profile_address", "flash_result"): "Hiện thông báo kết quả",

    ("order_review", "open_order"): "Mở chi tiết đơn hàng",
    ("order_review", "check_login"): "Khách đã đăng nhập?",
    ("order_review", "redirect_login"): "Yêu cầu đăng nhập trước",
    ("order_review", "load_order"): "Lấy thông tin đơn và vận chuyển",
    ("order_review", "order_found"): "Đơn thuộc về khách?",
    ("order_review", "not_found"): "Hiện trang không tìm thấy",
    ("order_review", "render_order"): "Hiện chi tiết đơn, vận chuyển và đánh giá",
    ("order_review", "can_review"): "Có sản phẩm được đánh giá?",
    ("order_review", "submit_review"): "Chọn sao và viết nhận xét",
    ("order_review", "post_review"): "Gửi đánh giá",
    ("order_review", "model_valid"): "Đánh giá hợp lệ?",
    ("order_review", "validate_review"): "Kiểm tra quyền đánh giá",
    ("order_review", "review_allowed"): "Được phép đánh giá?",
    ("order_review", "save_review"): "Lưu đánh giá sản phẩm",
    ("order_review", "refresh_rating"): "Cập nhật điểm đánh giá sản phẩm",
    ("order_review", "redirect_anchor"): "Quay lại phần đánh giá kèm thông báo",

    ("wishlist", "load_cards"): "Hiện trạng thái yêu thích trên sản phẩm",
    ("wishlist", "status_login"): "Khách đã đăng nhập?",
    ("wishlist", "status_result"): "Cập nhật biểu tượng yêu thích",
    ("wishlist", "click_toggle"): "Bấm thêm hoặc bỏ yêu thích",
    ("wishlist", "post_toggle"): "Gửi yêu cầu cập nhật yêu thích",
    ("wishlist", "toggle_login"): "Khách đã đăng nhập?",
    ("wishlist", "redirect_login"): "Yêu cầu đăng nhập trước",
    ("wishlist", "request_valid"): "Sản phẩm hợp lệ?",
    ("wishlist", "resolve_variant"): "Tìm tài khoản và sản phẩm",
    ("wishlist", "target_found"): "Có thể thao tác sản phẩm?",
    ("wishlist", "exists"): "Sản phẩm đã được yêu thích?",
    ("wishlist", "remove"): "Bỏ khỏi danh sách yêu thích",
    ("wishlist", "add"): "Thêm vào danh sách yêu thích",
    ("wishlist", "json_update"): "Cập nhật biểu tượng và thông báo",
    ("wishlist", "profile_remove"): "Bấm xóa trong hồ sơ",
    ("wishlist", "post_remove"): "Gửi yêu cầu xóa yêu thích",
    ("wishlist", "dom_remove"): "Ẩn sản phẩm khỏi danh sách",
    ("wishlist", "show_error"): "Báo lỗi cho khách",

    ("support_chat", "open_chat"): "Mở khung nhắn tin hỗ trợ",
    ("support_chat", "bootstrap"): "Chuẩn bị cuộc trò chuyện",
    ("support_chat", "load_bootstrap"): "Lấy thông tin khách và tin nhắn cũ",
    ("support_chat", "logged_in"): "Khách đã đăng nhập?",
    ("support_chat", "login_prompt"): "Mời khách đăng nhập để chat",
    ("support_chat", "render_messages"): "Hiện lịch sử trò chuyện",
    ("support_chat", "connect_realtime"): "Kết nối để nhận tin mới",
    ("support_chat", "connected"): "Kết nối thành công?",
    ("support_chat", "fallback_notice"): "Báo vẫn có thể gửi tin",
    ("support_chat", "send_message"): "Nhập và gửi tin nhắn",
    ("support_chat", "append_pending"): "Hiện tin nhắn đang gửi",
    ("support_chat", "send_signalr"): "Gửi tin qua kết nối trực tiếp",
    ("support_chat", "signalr_ok"): "Tin đã đến hệ thống hỗ trợ?",
    ("support_chat", "http_fallback"): "Gửi lại bằng cách dự phòng",
    ("support_chat", "persist_message"): "Lưu tin nhắn cho nhân viên",
    ("support_chat", "join_conversation"): "Theo dõi cuộc trò chuyện hiện tại",
    ("support_chat", "admin_reply"): "Nhân viên phản hồi",
    ("support_chat", "receive_reply"): "Hiện phản hồi cho khách",
    ("support_chat", "send_error"): "Báo gửi tin thất bại",

    ("ai_assistant", "open_ai"): "Mở trợ lý tư vấn",
    ("ai_assistant", "bootstrap_ai"): "Chuẩn bị khung tư vấn",
    ("ai_assistant", "load_ai_history"): "Lấy lịch sử tư vấn nếu có",
    ("ai_assistant", "connect_optional"): "Kết nối để lưu lịch sử",
    ("ai_assistant", "ask_question"): "Nhập câu hỏi cần tư vấn",
    ("ai_assistant", "post_chat"): "Gửi câu hỏi cho trợ lý",
    ("ai_assistant", "message_valid"): "Câu hỏi có nội dung?",
    ("ai_assistant", "bad_request"): "Nhắc khách nhập câu hỏi",
    ("ai_assistant", "find_products"): "Tìm sản phẩm phù hợp",
    ("ai_assistant", "build_context"): "Chuẩn bị thông tin để tư vấn",
    ("ai_assistant", "call_gemini"): "Trợ lý tạo câu trả lời",
    ("ai_assistant", "gemini_ok"): "Có câu trả lời?",
    ("ai_assistant", "service_error"): "Báo trợ lý đang bận",
    ("ai_assistant", "parse_reply"): "Chọn sản phẩm nên gợi ý",
    ("ai_assistant", "create_receipt"): "Chuẩn bị thông tin lưu lịch sử",
    ("ai_assistant", "render_reply"): "Hiện câu trả lời và sản phẩm gợi ý",
    ("ai_assistant", "save_history"): "Có thể lưu lịch sử?",
    ("ai_assistant", "persist_ai"): "Lưu cuộc tư vấn",
    ("ai_assistant", "login_note"): "Nhắc đăng nhập để lưu lịch sử",
    ("ai_assistant", "open_product"): "Mở sản phẩm được gợi ý",
}


NATURAL_GUARDS = {
    "Không / đã chọn": "Không hoặc đã chọn xong",
    "Rỗng + đăng nhập": "Giỏ trên máy trống, khách đã đăng nhập",
    "Có session + đăng nhập": "Có giỏ trên máy, khách đã đăng nhập",
    "Khách vãng lai": "Khách chưa đăng nhập",
    "Đổi voucher": "Đổi mã giảm giá",
    "Khi paid": "Khi đã thanh toán",
    "IPN nền": "Thông báo tự động",
    "Nếu ở hồ sơ": "Nếu thao tác trong hồ sơ",
}


def natural_node(flow: FlowSpec, node: NodeSpec) -> NodeSpec:
    return NodeSpec(
        node.key,
        node.kind,
        node.lane,
        NATURAL_NODE_NAMES.get((flow.key, node.key), node.name),
    )


def natural_edge(edge: EdgeSpec) -> EdgeSpec:
    return EdgeSpec(
        edge.source,
        edge.target,
        NATURAL_GUARDS.get(edge.guard, edge.guard),
    )


def natural_flow(flow: FlowSpec) -> FlowSpec:
    return FlowSpec(
        flow.key,
        NATURAL_FLOW_NAMES.get(flow.key, flow.name),
        NATURAL_DOCUMENTATION.get(flow.key, flow.documentation),
        flow.lanes,
        tuple(natural_node(flow, node) for node in flow.nodes),
        tuple(natural_edge(edge) for edge in flow.edges),
    )


FLOWS = tuple(natural_flow(flow) for flow in FLOWS)


def node_id(flow: FlowSpec, node: NodeSpec) -> str:
    return f"{slug(flow.key).upper()}_{slug(node.key).upper()}"


def partition_id(flow: FlowSpec, lane: str) -> str:
    return f"PARTITION_{slug(flow.key).upper()}_{slug(lane).upper()}"


def activity_id(flow: FlowSpec) -> str:
    return f"ACTIVITY_{slug(flow.key).upper()}"


def diagram_id(flow: FlowSpec) -> str:
    return f"DIAGRAM_{slug(flow.key).upper()}"


def model_node(flow: FlowSpec, node: NodeSpec) -> dict:
    type_by_kind = {
        "start": "UMLInitialNode",
        "final": "UMLActivityFinalNode",
        "decision": "UMLDecisionNode",
        "merge": "UMLMergeNode",
        "action": "UMLAction",
    }
    result = {
        "_type": type_by_kind[node.kind],
        "_id": node_id(flow, node),
        "_parent": ref(activity_id(flow)),
    }
    if node.name:
        result["name"] = node.name
    return result


def model_edge(flow: FlowSpec, edge: EdgeSpec, index: int) -> dict:
    edge_id = f"FLOW_{slug(flow.key).upper()}_{index:03d}"
    result = {
        "_type": "UMLControlFlow",
        "_id": edge_id,
        "_parent": ref(activity_id(flow)),
        "source": ref(f"{slug(flow.key).upper()}_{slug(edge.source).upper()}"),
        "target": ref(f"{slug(flow.key).upper()}_{slug(edge.target).upper()}"),
    }
    if edge.guard:
        result["guard"] = edge.guard
    return result


def lane_bounds(lanes: tuple[str, ...], max_steps: int) -> dict[str, tuple[int, int, int, int]]:
    height = max(520, NODE_TOP + max_steps * STEP_Y + 90)
    return {
        lane: (
            LANE_LEFT + index * (LANE_WIDTH + LANE_GAP),
            LANE_TOP,
            LANE_WIDTH,
            height,
        )
        for index, lane in enumerate(lanes)
    }


def text_width(text: str, minimum: int = 42, maximum: int = ACTION_MAX_WIDTH - ACTION_TEXT_PADDING) -> int:
    return max(minimum, min(maximum, int(len(text) * 6.6)))


def action_dimensions(text: str) -> tuple[int, int]:
    width = text_width(text) + ACTION_TEXT_PADDING
    return max(ACTION_MIN_WIDTH, min(ACTION_MAX_WIDTH, width)), ACTION_HEIGHT


def swimlane_view(flow: FlowSpec, lane: str, bounds: tuple[int, int, int, int]) -> dict:
    x, y, width, height = bounds
    view_id = f"VIEW_{partition_id(flow, lane)}"
    label_id = ids.new("label", f"{flow.key}_{lane}")
    return {
        "_type": "UMLSwimlaneView",
        "_id": view_id,
        "_parent": ref(diagram_id(flow)),
        "model": ref(partition_id(flow, lane)),
        "subViews": [
            {
                "_type": "LabelView",
                "_id": label_id,
                "_parent": ref(view_id),
                "fillColor": "#FFFFFF",
                "font": "Arial;13;1",
                "parentStyle": True,
                "left": x,
                "top": y + 4,
                "width": width,
                "height": 13,
                "text": lane,
                "verticalAlignment": 3,
            }
        ],
        "containedViews": [],
        "fillColor": "#FFFFFF",
        "font": "Arial;13;1",
        "parentStyle": False,
        "containerChangeable": True,
        "containerExtending": True,
        "left": x,
        "top": y,
        "width": width,
        "height": height,
        "nameLabel": ref(label_id),
    }


def node_position(
    flow: FlowSpec,
    node: NodeSpec,
    index: int,
    lane_map: dict[str, tuple[int, int, int, int]],
) -> tuple[int, int, int, int]:
    lane_x, _, lane_width, _ = lane_map[node.lane]
    y = NODE_TOP + index * STEP_Y
    if node.kind == "action":
        width, height = action_dimensions(node.name)
    elif node.kind == "decision":
        width, height = DECISION_WIDTH, DECISION_HEIGHT
    else:
        width = height = CONTROL_SIZE
    x = lane_x + (lane_width - width) // 2
    return x, y, width, height


def action_view(flow: FlowSpec, node: NodeSpec, box: tuple[int, int, int, int], lane_view_id: str) -> dict:
    x, y, width, height = box
    view_id = f"VIEW_{node_id(flow, node)}"
    compartment_id = ids.new("name_compartment", f"{flow.key}_{node.key}")
    stereotype_label = ids.new("label", f"{flow.key}_{node.key}_stereotype")
    name_label = ids.new("label", f"{flow.key}_{node.key}_name")
    namespace_label = ids.new("label", f"{flow.key}_{node.key}_namespace")
    property_label = ids.new("label", f"{flow.key}_{node.key}_property")
    label_width = text_width(node.name, minimum=40, maximum=width - 14)
    return {
        "_type": "UMLActionView",
        "_id": view_id,
        "_parent": ref(diagram_id(flow)),
        "model": ref(node_id(flow, node)),
        "subViews": [
            {
                "_type": "UMLNameCompartmentView",
                "_id": compartment_id,
                "_parent": ref(view_id),
                "model": ref(node_id(flow, node)),
                "subViews": [
                    {
                        "_type": "LabelView",
                        "_id": stereotype_label,
                        "_parent": ref(compartment_id),
                        "visible": False,
                        "fillColor": "#FFFFFF",
                        "font": "Arial;12;0",
                        "parentStyle": True,
                        "height": 12,
                    },
                    {
                        "_type": "LabelView",
                        "_id": name_label,
                        "_parent": ref(compartment_id),
                        "fillColor": "#FFFFFF",
                        "font": "Arial;12;1",
                        "parentStyle": True,
                        "left": x + (width - label_width) // 2,
                        "top": y + height // 2 - 6,
                        "width": label_width,
                        "height": 12,
                        "text": node.name,
                    },
                    {
                        "_type": "LabelView",
                        "_id": namespace_label,
                        "_parent": ref(compartment_id),
                        "visible": False,
                        "fillColor": "#FFFFFF",
                        "font": "Arial;12;0",
                        "parentStyle": True,
                        "height": 12,
                    },
                    {
                        "_type": "LabelView",
                        "_id": property_label,
                        "_parent": ref(compartment_id),
                        "visible": False,
                        "fillColor": "#FFFFFF",
                        "font": "Arial;12;0",
                        "parentStyle": True,
                        "height": 12,
                        "horizontalAlignment": 1,
                    },
                ],
                "fillColor": "#FFFFFF",
                "font": "Arial;12;0",
                "parentStyle": True,
                "left": x + 6,
                "top": y + 13,
                "width": width - 12,
                "height": 24,
                "stereotypeLabel": ref(stereotype_label),
                "nameLabel": ref(name_label),
                "namespaceLabel": ref(namespace_label),
                "propertyLabel": ref(property_label),
            }
        ],
        "containerView": ref(lane_view_id),
        "fillColor": "#FFFFFF",
        "font": "Arial;12;0",
        "parentStyle": False,
        "containerChangeable": True,
        "containerExtending": True,
        "left": x,
        "top": y,
        "width": width,
        "height": height,
        "nameCompartment": ref(compartment_id),
    }


def control_node_view(flow: FlowSpec, node: NodeSpec, box: tuple[int, int, int, int], lane_view_id: str) -> dict:
    x, y, width, height = box
    view_id = f"VIEW_{node_id(flow, node)}"
    view = {
        "_type": "UMLControlNodeView",
        "_id": view_id,
        "_parent": ref(diagram_id(flow)),
        "model": ref(node_id(flow, node)),
        "containerView": ref(lane_view_id),
        "fillColor": "#FFFFFF",
        "font": "Arial;12;0",
        "parentStyle": False,
        "containerChangeable": True,
        "containerExtending": True,
        "left": x,
        "top": y,
        "width": width,
        "height": height,
    }

    return view


def center(box: tuple[int, int, int, int]) -> tuple[int, int]:
    x, y, width, height = box
    return x + width // 2, y + height // 2


def edge_points(
    source_box: tuple[int, int, int, int],
    target_box: tuple[int, int, int, int],
) -> str:
    sx, sy = center(source_box)
    tx, ty = center(target_box)
    if abs(sx - tx) < 8:
        return f"{sx}:{sy};{tx}:{ty}"
    mid_y = sy + (ty - sy) // 2
    return f"{sx}:{sy};{sx}:{mid_y};{tx}:{mid_y};{tx}:{ty}"


def edge_label_text(edge: EdgeSpec) -> str:
    return f" [{edge.guard}]" if edge.guard else ""


def edge_view(
    flow: FlowSpec,
    edge: EdgeSpec,
    edge_model: dict,
    source_box: tuple[int, int, int, int],
    target_box: tuple[int, int, int, int],
) -> dict:
    source_view_id = f"VIEW_{slug(flow.key).upper()}_{slug(edge.source).upper()}"
    target_view_id = f"VIEW_{slug(flow.key).upper()}_{slug(edge.target).upper()}"
    view_id = f"VIEW_{edge_model['_id']}"
    name_label = ids.new("edge_label", f"{flow.key}_{edge.source}_{edge.target}_name")
    stereotype_label = ids.new("edge_label", f"{flow.key}_{edge.source}_{edge.target}_stereotype")
    property_label = ids.new("edge_label", f"{flow.key}_{edge.source}_{edge.target}_property")
    sx, sy = center(source_box)
    tx, ty = center(target_box)
    label = edge_label_text(edge)
    label_x = sx + (tx - sx) // 2
    label_y = sy + (ty - sy) // 2
    return {
        "_type": "UMLControlFlowView",
        "_id": view_id,
        "_parent": ref(diagram_id(flow)),
        "model": ref(edge_model["_id"]),
        "subViews": [
            {
                "_type": "EdgeLabelView",
                "_id": name_label,
                "_parent": ref(view_id),
                "model": ref(edge_model["_id"]),
                "visible": bool(label),
                "fillColor": "#FFFFFF",
                "font": "Arial;13;0",
                "parentStyle": False,
                "left": label_x,
                "top": label_y,
                "width": text_width(label, 20, 180),
                "height": 13,
                "alpha": math.pi / 2,
                "distance": 15,
                "hostEdge": ref(view_id),
                "edgePosition": 1,
                "text": label,
            },
            {
                "_type": "EdgeLabelView",
                "_id": stereotype_label,
                "_parent": ref(view_id),
                "model": ref(edge_model["_id"]),
                "visible": None,
                "fillColor": "#FFFFFF",
                "font": "Arial;13;0",
                "parentStyle": False,
                "left": label_x + 20,
                "top": label_y,
                "height": 13,
                "alpha": math.pi / 2,
                "distance": 30,
                "hostEdge": ref(view_id),
                "edgePosition": 1,
            },
            {
                "_type": "EdgeLabelView",
                "_id": property_label,
                "_parent": ref(view_id),
                "model": ref(edge_model["_id"]),
                "visible": False,
                "fillColor": "#FFFFFF",
                "font": "Arial;13;0",
                "parentStyle": False,
                "left": label_x - 20,
                "top": label_y,
                "height": 13,
                "alpha": -math.pi / 2,
                "distance": 15,
                "hostEdge": ref(view_id),
                "edgePosition": 1,
            },
        ],
        "fillColor": "#FFFFFF",
        "font": "Arial;12;0",
        "parentStyle": False,
        "containerExtending": True,
        "head": ref(target_view_id),
        "tail": ref(source_view_id),
        "lineStyle": RECTILINEAR_LINE_STYLE,
        "points": edge_points(source_box, target_box),
        "showVisibility": True,
        "nameLabel": ref(name_label),
        "stereotypeLabel": ref(stereotype_label),
        "propertyLabel": ref(property_label),
    }


def build_activity(flow: FlowSpec, default: bool) -> dict:
    activity = {
        "_type": "UMLActivity",
        "_id": activity_id(flow),
        "_parent": ref(MODEL_ID),
        "name": flow.name,
        "ownedElements": [],
        "documentation": flow.documentation,
        "nodes": [],
        "edges": [],
        "groups": [],
    }

    lane_map = lane_bounds(flow.lanes, len(flow.nodes))
    lane_views = {lane: swimlane_view(flow, lane, bounds) for lane, bounds in lane_map.items()}
    for lane in flow.lanes:
        activity["groups"].append({
            "_type": "UMLActivityPartition",
            "_id": partition_id(flow, lane),
            "_parent": ref(activity_id(flow)),
            "name": lane,
        })

    diagram = {
        "_type": "UMLActivityDiagram",
        "_id": diagram_id(flow),
        "_parent": ref(activity_id(flow)),
        "name": flow.name,
        "defaultDiagram": default,
        "ownedViews": list(lane_views.values()),
    }

    box_by_key: dict[str, tuple[int, int, int, int]] = {}
    for index, node in enumerate(flow.nodes):
        activity["nodes"].append(model_node(flow, node))
        box = node_position(flow, node, index, lane_map)
        box_by_key[node.key] = box
        lane_view_id = lane_views[node.lane]["_id"]
        if node.kind == "action":
            view = action_view(flow, node, box, lane_view_id)
            diagram["ownedViews"].append(view)
            lane_views[node.lane]["containedViews"].append(ref(view["_id"]))
        else:
            view = control_node_view(flow, node, box, lane_view_id)
            diagram["ownedViews"].append(view)
            lane_views[node.lane]["containedViews"].append(ref(view["_id"]))

    for index, edge in enumerate(flow.edges, start=1):
        model = model_edge(flow, edge, index)
        activity["edges"].append(model)
        diagram["ownedViews"].append(edge_view(
            flow,
            edge,
            model,
            box_by_key[edge.source],
            box_by_key[edge.target],
        ))

    activity["ownedElements"].append(diagram)
    return activity


def build_project() -> dict:
    return {
        "_type": "Project",
        "_id": PROJECT_ID,
        "name": "E-Commerce Customer - Activity Diagrams",
        "ownedElements": [
            {
                "_type": "UMLModel",
                "_id": MODEL_ID,
                "_parent": ref(PROJECT_ID),
                "name": "Activity Diagrams",
                "ownedElements": [
                    build_activity(flow, default=index == 0)
                    for index, flow in enumerate(FLOWS)
                ],
            }
        ],
    }


def build_markdown() -> str:
    flow_lines = "\n".join(
        f"- `{flow.name}`: {flow.documentation}"
        for flow in FLOWS
    )
    basis_lines = "\n".join([
        "- Các bước khách nhìn thấy và thao tác trực tiếp trên màn hình.",
        "- Các phản hồi mà hệ thống hiển thị lại cho khách khi thành công, thiếu thông tin hoặc có lỗi.",
        "- Các nhánh đặc biệt trong mua hàng: mua ngay, chọn một phần giỏ hàng, thanh toán khi nhận hàng, thanh toán online và chuyển khoản.",
        "- Các luồng sau mua: xem đơn hàng, theo dõi vận chuyển, gửi đánh giá, lưu sản phẩm yêu thích, nhắn tin hỗ trợ và hỏi trợ lý tư vấn.",
    ])
    notes = "\n".join([
        "- Ngôn ngữ trong sơ đồ ưu tiên cách nói tự nhiên để người không biết kỹ thuật vẫn đọc được.",
        "- Giỏ hàng và danh sách món được chọn để thanh toán được hiểu là hai bước khác nhau: chọn món để đặt hàng không làm mất món còn lại trong giỏ.",
        "- Với thanh toán online, đơn được tạo trước rồi hệ thống chờ kết quả thanh toán để xác nhận hoặc hủy đơn.",
        "- Với chuyển khoản SePay, khách xem hướng dẫn chuyển khoản, còn hệ thống tự đối chiếu giao dịch ngân hàng để cập nhật đơn.",
        "- Đánh giá sau mua chỉ xuất hiện khi đơn đã hoàn tất và đúng sản phẩm khách đã mua.",
    ])
    return f"""# Activity Diagram Web Khách Hàng

File StarUML chính: `e-commerce-customer-activity.mdj`

## Danh sách sơ đồ
{flow_lines}

## Căn cứ trình bày luồng
{basis_lines}

## Ghi chú khi đọc sơ đồ
{notes}
"""


def main() -> None:
    project = build_project()
    MDJ_PATH.write_text(json.dumps(project, ensure_ascii=False, indent=2), encoding="utf-8")
    MD_PATH.write_text(build_markdown(), encoding="utf-8")


if __name__ == "__main__":
    main()
