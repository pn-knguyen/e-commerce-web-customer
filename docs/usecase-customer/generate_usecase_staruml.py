# -*- coding: utf-8 -*-
"""Generate StarUML use-case diagrams for the customer storefront."""

from __future__ import annotations

import json
import math
import re
from pathlib import Path


BASE_DIR = Path(__file__).resolve().parent
MDJ_PATH = BASE_DIR / "customer-web-usecase-staruml.mdj"
MD_PATH = BASE_DIR / "customer-web-usecase.md"


def slug(value: str) -> str:
    cleaned = re.sub(r"[^a-zA-Z0-9]+", "_", value.strip().lower()).strip("_")
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
PROJECT_ID = "project_techstore_customer_usecase"
MODEL_ID = "model_techstore_customer_usecase"


ACTORS = {
    "guest": "Khách vãng lai",
    "customer": "Khách hàng đã đăng nhập",
    "firebase": "Firebase Auth",
    "momo": "Cổng thanh toán MoMo",
    "vnpay": "Cổng thanh toán VNPay",
    "sepay": "Ngân hàng / SePay",
    "payment_gateway": "Cổng thanh toán online",
    "sepay_webhook": "Webhook SePay",
    "province_api": "API địa giới Việt Nam",
    "gemini": "Gemini AI",
    "support_admin": "Admin hỗ trợ",
    "realtime": "Hệ thống chat realtime",
    "shipping": "Đơn vị vận chuyển / GHN",
}


USE_CASES = {
    "shopping_overview": "Khám phá và mua sắm sản phẩm",
    "account_overview": "Xác thực và quản lý tài khoản",
    "wishlist_overview": "Quản lý sản phẩm yêu thích",
    "cart_overview": "Quản lý giỏ hàng",
    "checkout_overview": "Đặt hàng và thanh toán",
    "order_aftercare_overview": "Theo dõi đơn hàng và đánh giá",
    "support_ai_overview": "Hỗ trợ khách hàng và tư vấn AI",
    "home": "Xem trang chủ",
    "browse_menu": "Duyệt menu danh mục",
    "view_campaigns": "Xem banner, slide, khuyến mãi",
    "home_sections": "Xem section sản phẩm trang chủ",
    "accessory_directory": "Xem thư mục phụ kiện",
    "search": "Tìm kiếm sản phẩm",
    "search_suggest": "Xem gợi ý tìm kiếm",
    "search_history": "Lưu lịch sử tìm kiếm",
    "search_results": "Xem kết quả tìm kiếm",
    "search_filter_sort": "Lọc / sắp xếp kết quả tìm kiếm",
    "catalog": "Xem catalog sản phẩm",
    "catalog_sectioned": "Xem catalog theo section",
    "catalog_filters": "Lọc catalog theo brand, tồn kho, thuộc tính",
    "catalog_sort": "Sắp xếp catalog",
    "catalog_hot_sale": "Xem hot sale, brand, quick link",
    "catalog_section_products": "Tải sản phẩm theo section",
    "product_detail": "Xem chi tiết sản phẩm",
    "choose_variant": "Chọn phiên bản / màu sắc",
    "view_gallery": "Xem ảnh / gallery",
    "view_specs": "Xem thông số kỹ thuật",
    "view_reviews": "Xem đánh giá sản phẩm",
    "view_related": "Xem sản phẩm / phiên bản liên quan",
    "view_upsell": "Xem phụ kiện mua kèm",
    "view_qa": "Xem Q&A và nội dung SEO",
    "register": "Đăng ký tài khoản",
    "login": "Đăng nhập",
    "login_email": "Đăng nhập email / mật khẩu",
    "login_social": "Đăng nhập Google / Facebook",
    "login_phone": "Đăng nhập SMS OTP",
    "login_magic": "Đăng nhập Magic Link",
    "firebase_sync": "Đồng bộ phiên Firebase với backend",
    "forgot_password": "Quên / khôi phục mật khẩu",
    "logout": "Đăng xuất",
    "profile": "Xem hồ sơ cá nhân",
    "profile_overview": "Xem tổng quan tài khoản",
    "profile_history": "Xem lịch sử mua hàng",
    "profile_order_detail": "Xem chi tiết đơn hàng",
    "profile_notifications": "Xem thông báo tài khoản",
    "address_book": "Quản lý sổ địa chỉ",
    "add_address": "Thêm địa chỉ giao hàng",
    "set_default_address": "Đặt địa chỉ mặc định",
    "profile_favorites": "Xem sản phẩm yêu thích",
    "cart_view": "Xem giỏ hàng",
    "cart_count": "Xem số lượng giỏ hàng",
    "cart_add": "Thêm sản phẩm vào giỏ",
    "cart_buy_now": "Mua ngay",
    "cart_update_qty": "Tăng / giảm số lượng",
    "cart_remove": "Xóa sản phẩm khỏi giỏ",
    "cart_remove_selected": "Xóa nhiều sản phẩm",
    "cart_select_checkout": "Chọn sản phẩm thanh toán",
    "cart_recommendations": "Chọn phụ kiện gợi ý trong giỏ",
    "cart_persist": "Lưu / đồng bộ giỏ hàng",
    "cart_validate": "Kiểm tra hợp lệ và tồn kho",
    "checkout_prepare": "Chuẩn bị checkout",
    "checkout_page": "Vào trang checkout",
    "checkout_default_address": "Dùng địa chỉ mặc định",
    "checkout_shipping": "Nhập thông tin giao hàng",
    "checkout_location": "Chọn tỉnh / huyện / xã",
    "checkout_payment_method": "Chọn phương thức thanh toán",
    "checkout_vouchers": "Xem voucher khả dụng",
    "checkout_apply_voucher": "Áp dụng voucher",
    "checkout_validate_voucher": "Xác thực voucher",
    "place_order": "Đặt hàng",
    "create_order": "Tạo đơn hàng",
    "checkout_success": "Xem trang đặt hàng thành công",
    "pay_cod": "Thanh toán COD",
    "pay_momo": "Thanh toán MoMo",
    "pay_vnpay": "Thanh toán VNPay",
    "pay_sepay": "Thanh toán SePay",
    "payment_create_url": "Tạo URL / phiên thanh toán",
    "payment_return": "Nhận redirect kết quả thanh toán",
    "payment_ipn": "Nhận IPN thanh toán",
    "payment_result": "Hiển thị kết quả thanh toán",
    "payment_confirm": "Xác nhận thanh toán online",
    "payment_cancel_failed": "Hủy đơn thanh toán thất bại",
    "sepay_instruction": "Xem hướng dẫn chuyển khoản SePay",
    "sepay_copy": "Sao chép thông tin chuyển khoản",
    "sepay_status": "Kiểm tra trạng thái SePay",
    "sepay_cancel": "Hủy thanh toán SePay",
    "sepay_webhook_receive": "Nhận webhook SePay",
    "order_status": "Theo dõi trạng thái đơn",
    "shipment_timeline": "Xem timeline vận chuyển",
    "order_support_info": "Xem thông tin hỗ trợ cửa hàng",
    "review_after_purchase": "Viết đánh giá sau mua",
    "submit_review": "Gửi đánh giá đơn hàng",
    "wishlist_status": "Kiểm tra trạng thái yêu thích",
    "wishlist_toggle": "Thêm / bỏ yêu thích",
    "wishlist_remove": "Xóa sản phẩm yêu thích",
    "support_open": "Mở chat admin",
    "chat_bootstrap": "Bootstrap hội thoại",
    "support_login_prompt": "Đăng nhập để chat admin",
    "support_send": "Gửi tin nhắn admin",
    "realtime_connect": "Kết nối realtime",
    "support_http_fallback": "Gửi qua HTTP fallback",
    "support_receive": "Nhận phản hồi admin",
    "realtime_token": "Lấy token realtime",
    "ai_open": "Mở trợ lý AI",
    "ai_ask": "Hỏi AI tư vấn",
    "ai_history": "Gửi lịch sử hội thoại",
    "ai_reply": "Nhận câu trả lời AI",
    "ai_products": "Nhận gợi ý sản phẩm",
    "ai_save": "Lưu lịch sử tư vấn AI",
    "ai_product_detail": "Xem sản phẩm AI gợi ý",
}


actor_ids = {key: f"actor_{slug(key)}" for key in ACTORS}
uc_ids = {key: f"uc_{slug(key)}" for key in USE_CASES}


def make_actor(key: str, name: str) -> dict:
    return {
        "_type": "UMLActor",
        "_id": actor_ids[key],
        "_parent": ref(MODEL_ID),
        "name": name,
        "ownedElements": [],
    }


def make_use_case(key: str, name: str) -> dict:
    return {
        "_type": "UMLUseCase",
        "_id": uc_ids[key],
        "_parent": ref(MODEL_ID),
        "name": name,
        "ownedElements": [],
    }


actors = {key: make_actor(key, name) for key, name in ACTORS.items()}
use_cases = {key: make_use_case(key, name) for key, name in USE_CASES.items()}


def add_owned(owner_id: str, element: dict) -> None:
    if owner_id in actor_ids.values():
        owner = next(actor for actor in actors.values() if actor["_id"] == owner_id)
    elif owner_id in uc_ids.values():
        owner = next(use_case for use_case in use_cases.values() if use_case["_id"] == owner_id)
    else:
        raise KeyError(owner_id)
    owner.setdefault("ownedElements", []).append(element)


def association_model(source_id: str, target_id: str, key: str) -> dict:
    assoc_id = ids.new("assoc", key)
    model = {
        "_type": "UMLAssociation",
        "_id": assoc_id,
        "_parent": ref(source_id),
        "end1": {
            "_type": "UMLAssociationEnd",
            "_id": ids.new("assoc_end1", key),
            "_parent": ref(assoc_id),
            "reference": ref(source_id),
            "navigable": False,
        },
        "end2": {
            "_type": "UMLAssociationEnd",
            "_id": ids.new("assoc_end2", key),
            "_parent": ref(assoc_id),
            "reference": ref(target_id),
        },
    }
    add_owned(source_id, model)
    return model


def include_model(source_uc_id: str, target_uc_id: str, key: str) -> dict:
    include_id = ids.new("include", key)
    model = {
        "_type": "UMLInclude",
        "_id": include_id,
        "_parent": ref(source_uc_id),
        "source": ref(source_uc_id),
        "target": ref(target_uc_id),
    }
    add_owned(source_uc_id, model)
    return model


def extend_model(source_uc_id: str, target_uc_id: str, key: str) -> dict:
    extend_id = ids.new("extend", key)
    model = {
        "_type": "UMLExtend",
        "_id": extend_id,
        "_parent": ref(source_uc_id),
        "source": ref(source_uc_id),
        "target": ref(target_uc_id),
    }
    add_owned(source_uc_id, model)
    return model


def generalization_model(source_id: str, target_id: str, key: str) -> dict:
    generalization_id = ids.new("gen", key)
    model = {
        "_type": "UMLGeneralization",
        "_id": generalization_id,
        "_parent": ref(source_id),
        "source": ref(source_id),
        "target": ref(target_id),
    }
    add_owned(source_id, model)
    return model


def label_view(parent: str, text: str, left: float, top: float, width: float, bold: bool = True, visible: bool | None = None) -> dict:
    data = {
        "_type": "LabelView",
        "_id": ids.new("label", f"{parent}_{text}"),
        "_parent": ref(parent),
        "font": f"Arial;13;{1 if bold else 0}",
        "left": left,
        "top": top,
        "width": width,
        "height": 13,
        "text": text,
    }
    if visible is not None:
        data["visible"] = visible
    return data


def hidden_label(parent: str, left: float, top: float, width: float = 10) -> dict:
    return {
        "_type": "LabelView",
        "_id": ids.new("hidden_label", parent),
        "_parent": ref(parent),
        "visible": False,
        "font": "Arial;13;0",
        "left": left,
        "top": top,
        "width": width,
        "height": 13,
    }


def name_compartment(parent: str, model_id: str, text: str, left: float, top: float, width: float) -> tuple[dict, str]:
    compartment_id = ids.new("name_comp", f"{parent}_{text}")
    stereotype = hidden_label(compartment_id, left - 240, top - 240)
    name_label = label_view(compartment_id, text, left + 5, top + 7, max(width - 10, 30), True)
    namespace = hidden_label(compartment_id, left - 240, top - 240, 73)
    prop = hidden_label(compartment_id, left - 240, top - 240)
    compartment = {
        "_type": "UMLNameCompartmentView",
        "_id": compartment_id,
        "_parent": ref(parent),
        "model": ref(model_id),
        "subViews": [stereotype, name_label, namespace, prop],
        "font": "Arial;13;0",
        "left": left,
        "top": top,
        "width": width,
        "height": 25,
        "stereotypeLabel": ref(stereotype["_id"]),
        "nameLabel": ref(name_label["_id"]),
        "namespaceLabel": ref(namespace["_id"]),
        "propertyLabel": ref(prop["_id"]),
    }
    return compartment, compartment_id


def compartment_view(kind: str, parent: str, model_id: str) -> dict:
    return {
        "_type": kind,
        "_id": ids.new(slug(kind), parent),
        "_parent": ref(parent),
        "model": ref(model_id),
        "visible": False,
        "font": "Arial;13;0",
        "left": -80,
        "top": -80,
        "width": 10,
        "height": 10,
    }


def usecase_view(diagram_id: str, model_id: str, name: str, left: int, top: int, width: int = 180, height: int = 48) -> dict:
    view_id = ids.new("view_uc", f"{diagram_id}_{model_id}")
    name_comp, name_id = name_compartment(view_id, model_id, name, left + 10, top + math.floor((height - 25) / 2), width - 20)
    attr = compartment_view("UMLAttributeCompartmentView", view_id, model_id)
    op = compartment_view("UMLOperationCompartmentView", view_id, model_id)
    rec = compartment_view("UMLReceptionCompartmentView", view_id, model_id)
    templ = compartment_view("UMLTemplateParameterCompartmentView", view_id, model_id)
    ext = compartment_view("UMLExtensionPointCompartmentView", view_id, model_id)
    return {
        "_type": "UMLUseCaseView",
        "_id": view_id,
        "_parent": ref(diagram_id),
        "model": ref(model_id),
        "subViews": [name_comp, attr, op, rec, templ, ext],
        "font": "Arial;13;0",
        "containerChangeable": True,
        "left": left,
        "top": top,
        "width": width,
        "height": height,
        "nameCompartment": ref(name_id),
        "suppressAttributes": True,
        "suppressOperations": True,
        "attributeCompartment": ref(attr["_id"]),
        "operationCompartment": ref(op["_id"]),
        "receptionCompartment": ref(rec["_id"]),
        "templateParameterCompartment": ref(templ["_id"]),
        "extensionPointCompartment": ref(ext["_id"]),
    }


def actor_view(diagram_id: str, model_id: str, name: str, left: int, top: int, width: int = 145, height: int = 82) -> dict:
    view_id = ids.new("view_actor", f"{diagram_id}_{model_id}")
    name_comp, name_id = name_compartment(view_id, model_id, name, left, top + 56, width)
    attr = compartment_view("UMLAttributeCompartmentView", view_id, model_id)
    op = compartment_view("UMLOperationCompartmentView", view_id, model_id)
    rec = compartment_view("UMLReceptionCompartmentView", view_id, model_id)
    templ = compartment_view("UMLTemplateParameterCompartmentView", view_id, model_id)
    return {
        "_type": "UMLActorView",
        "_id": view_id,
        "_parent": ref(diagram_id),
        "model": ref(model_id),
        "subViews": [name_comp, attr, op, rec, templ],
        "font": "Arial;13;0",
        "containerChangeable": True,
        "left": left,
        "top": top,
        "width": width,
        "height": height,
        "nameCompartment": ref(name_id),
        "suppressAttributes": True,
        "suppressOperations": True,
        "attributeCompartment": ref(attr["_id"]),
        "operationCompartment": ref(op["_id"]),
        "receptionCompartment": ref(rec["_id"]),
        "templateParameterCompartment": ref(templ["_id"]),
    }


def subject_view(diagram_id: str, name: str, left: int, top: int, width: int, height: int) -> tuple[dict, dict]:
    subject_id = ids.new("subject", diagram_id)
    subject_element = {
        "_type": "UMLUseCaseSubject",
        "_id": subject_id,
        "_parent": ref(MODEL_ID),
        "name": name,
    }
    view_id = ids.new("view_subject", diagram_id)
    name_comp, name_id = name_compartment(view_id, subject_id, name, left, top, width)
    view = {
        "_type": "UMLUseCaseSubjectView",
        "_id": view_id,
        "_parent": ref(diagram_id),
        "model": ref(subject_id),
        "subViews": [name_comp],
        "font": "Arial;13;0",
        "left": left,
        "top": top,
        "width": width,
        "height": height,
        "nameCompartment": ref(name_id),
    }
    return subject_element, view


def edge_label(parent: str, model_id: str, text: str | None, mid_x: float, mid_y: float) -> tuple[list[dict], str, str, str]:
    name = {
        "_type": "EdgeLabelView",
        "_id": ids.new("edge_name", parent),
        "_parent": ref(parent),
        "model": ref(model_id),
        "visible": False,
        "font": "Arial;13;0",
        "left": mid_x,
        "top": mid_y - 16,
        "height": 13,
        "alpha": 1.5707963267948966,
        "distance": 15,
        "hostEdge": ref(parent),
        "edgePosition": 1,
    }
    stereotype = {
        "_type": "EdgeLabelView",
        "_id": ids.new("edge_stereo", parent),
        "_parent": ref(parent),
        "model": ref(model_id),
        "visible": None if text is None else True,
        "font": "Arial;13;0",
        "left": mid_x - 28,
        "top": mid_y - 6,
        "width": 66,
        "height": 13,
        "alpha": 0,
        "distance": 5,
        "hostEdge": ref(parent),
        "edgePosition": 1,
    }
    if text:
        stereotype["text"] = text
    prop = {
        "_type": "EdgeLabelView",
        "_id": ids.new("edge_prop", parent),
        "_parent": ref(parent),
        "model": ref(model_id),
        "visible": False,
        "font": "Arial;13;0",
        "left": mid_x,
        "top": mid_y + 14,
        "height": 13,
        "alpha": -1.5707963267948966,
        "distance": 15,
        "hostEdge": ref(parent),
        "edgePosition": 1,
    }
    return [name, stereotype, prop], name["_id"], stereotype["_id"], prop["_id"]


def edge_view(
    diagram_id: str,
    rel_model: dict,
    tail_view_id: str,
    head_view_id: str,
    tail_xy: tuple[float, float],
    head_xy: tuple[float, float],
    view_type: str,
    stereotype_text: str | None = None,
) -> dict:
    view_id = ids.new("view_edge", f"{diagram_id}_{rel_model['_id']}")
    mid_x = (tail_xy[0] + head_xy[0]) / 2
    mid_y = (tail_xy[1] + head_xy[1]) / 2
    labels, name_id, stereotype_id, prop_id = edge_label(view_id, rel_model["_id"], stereotype_text, mid_x, mid_y)
    return {
        "_type": view_type,
        "_id": view_id,
        "_parent": ref(diagram_id),
        "model": ref(rel_model["_id"]),
        "subViews": labels,
        "font": "Arial;13;0",
        "head": ref(head_view_id),
        "tail": ref(tail_view_id),
        "lineStyle": 1,
        "points": f"{round(tail_xy[0])}:{round(tail_xy[1])};{round(head_xy[0])}:{round(head_xy[1])}",
        "showVisibility": True,
        "nameLabel": ref(name_id),
        "stereotypeLabel": ref(stereotype_id),
        "propertyLabel": ref(prop_id),
    }


def center(box: tuple[int, int, int, int]) -> tuple[float, float]:
    left, top, width, height = box
    return (left + width / 2, top + height / 2)


def make_diagram(spec: dict) -> tuple[dict, list[dict]]:
    diagram_id = f"diagram_{slug(spec['name'])}"
    diagram = {
        "_type": "UMLUseCaseDiagram",
        "_id": diagram_id,
        "_parent": ref(MODEL_ID),
        "name": spec["name"],
        "ownedViews": [],
        "defaultDiagram": spec.get("default", False),
    }
    subject_element, subject_box = subject_view(
        diagram_id,
        spec.get("subject", "TechStore Web Khách hàng"),
        *spec.get("boundary", (220, 48, 820, 820)),
    )
    diagram["ownedViews"].append(subject_box)

    view_by_key: dict[str, dict] = {}
    box_by_key: dict[str, tuple[int, int, int, int]] = {}

    for key, box in spec.get("actors", {}).items():
        actor = actors[key]
        left, top, width, height = box
        view = actor_view(diagram_id, actor["_id"], actor["name"], left, top, width, height)
        diagram["ownedViews"].append(view)
        view_by_key[f"actor:{key}"] = view
        box_by_key[f"actor:{key}"] = box

    for key, box in spec.get("usecases", {}).items():
        use_case = use_cases[key]
        left, top, width, height = box
        view = usecase_view(diagram_id, use_case["_id"], use_case["name"], left, top, width, height)
        diagram["ownedViews"].append(view)
        view_by_key[f"uc:{key}"] = view
        box_by_key[f"uc:{key}"] = box

    for source, target in spec.get("associations", []):
        if source not in view_by_key or target not in view_by_key:
            continue
        source_kind, source_key = source.split(":", 1)
        target_kind, target_key = target.split(":", 1)
        source_id = actor_ids[source_key] if source_kind == "actor" else uc_ids[source_key]
        target_id = actor_ids[target_key] if target_kind == "actor" else uc_ids[target_key]
        rel = association_model(source_id, target_id, f"{diagram_id}_{source}_{target}")
        diagram["ownedViews"].append(edge_view(
            diagram_id,
            rel,
            view_by_key[source]["_id"],
            view_by_key[target]["_id"],
            center(box_by_key[source]),
            center(box_by_key[target]),
            "UMLAssociationView",
        ))

    for source_key, target_key in spec.get("includes", []):
        source = f"uc:{source_key}"
        target = f"uc:{target_key}"
        if source not in view_by_key or target not in view_by_key:
            continue
        rel = include_model(uc_ids[source_key], uc_ids[target_key], f"{diagram_id}_{source_key}_{target_key}")
        diagram["ownedViews"].append(edge_view(
            diagram_id,
            rel,
            view_by_key[source]["_id"],
            view_by_key[target]["_id"],
            center(box_by_key[source]),
            center(box_by_key[target]),
            "UMLIncludeView",
            "«include»",
        ))

    for source_key, target_key in spec.get("extends", []):
        source = f"uc:{source_key}"
        target = f"uc:{target_key}"
        if source not in view_by_key or target not in view_by_key:
            continue
        rel = extend_model(uc_ids[source_key], uc_ids[target_key], f"{diagram_id}_{source_key}_{target_key}")
        diagram["ownedViews"].append(edge_view(
            diagram_id,
            rel,
            view_by_key[source]["_id"],
            view_by_key[target]["_id"],
            center(box_by_key[source]),
            center(box_by_key[target]),
            "UMLExtendView",
            "«extend»",
        ))

    for source, target in spec.get("generalizations", []):
        if source not in view_by_key or target not in view_by_key:
            continue
        source_kind, source_key = source.split(":", 1)
        target_kind, target_key = target.split(":", 1)
        source_id = actor_ids[source_key] if source_kind == "actor" else uc_ids[source_key]
        target_id = actor_ids[target_key] if target_kind == "actor" else uc_ids[target_key]
        rel = generalization_model(source_id, target_id, f"{diagram_id}_{source}_{target}")
        diagram["ownedViews"].append(edge_view(
            diagram_id,
            rel,
            view_by_key[source]["_id"],
            view_by_key[target]["_id"],
            center(box_by_key[source]),
            center(box_by_key[target]),
            "UMLGeneralizationView",
        ))

    return diagram, [subject_element]


UC_W = 185
UC_H = 50
ACTOR_W = 150
ACTOR_H = 84


DIAGRAMS = [
    {
        "name": "01 Tổng quan Web khách hàng",
        "default": True,
        "boundary": (225, 45, 810, 720),
        "actors": {
            "guest": (35, 170, ACTOR_W, ACTOR_H),
            "customer": (35, 390, ACTOR_W, ACTOR_H),
            "firebase": (1080, 70, 170, ACTOR_H),
            "sepay": (1080, 230, 170, ACTOR_H),
            "gemini": (1080, 400, 170, ACTOR_H),
            "support_admin": (1080, 555, 170, ACTOR_H),
            "shipping": (1080, 690, 170, ACTOR_H),
        },
        "usecases": {
            "home": (270, 95, UC_W, UC_H),
            "browse_menu": (520, 95, UC_W, UC_H),
            "search": (770, 95, UC_W, UC_H),
            "catalog": (270, 190, UC_W, UC_H),
            "product_detail": (520, 190, UC_W, UC_H),
            "view_reviews": (770, 190, UC_W, UC_H),
            "cart_view": (270, 285, UC_W, UC_H),
            "cart_buy_now": (520, 285, UC_W, UC_H),
            "place_order": (770, 285, UC_W, UC_H),
            "login": (270, 380, UC_W, UC_H),
            "profile": (520, 380, UC_W, UC_H),
            "wishlist_toggle": (770, 380, UC_W, UC_H),
            "profile_order_detail": (270, 475, UC_W, UC_H),
            "shipment_timeline": (520, 475, UC_W, UC_H),
            "review_after_purchase": (770, 475, UC_W, UC_H),
            "support_open": (270, 595, UC_W, UC_H),
            "ai_ask": (520, 595, UC_W, UC_H),
            "ai_products": (770, 595, UC_W, UC_H),
        },
        "generalizations": [("actor:customer", "actor:guest")],
        "associations": [
            ("actor:guest", "uc:home"),
            ("actor:guest", "uc:browse_menu"),
            ("actor:guest", "uc:search"),
            ("actor:guest", "uc:catalog"),
            ("actor:guest", "uc:product_detail"),
            ("actor:guest", "uc:cart_view"),
            ("actor:guest", "uc:cart_buy_now"),
            ("actor:guest", "uc:support_open"),
            ("actor:guest", "uc:ai_ask"),
            ("actor:customer", "uc:profile"),
            ("actor:customer", "uc:wishlist_toggle"),
            ("actor:customer", "uc:place_order"),
            ("actor:customer", "uc:profile_order_detail"),
            ("actor:customer", "uc:review_after_purchase"),
            ("actor:firebase", "uc:login"),
            ("actor:sepay", "uc:place_order"),
            ("actor:gemini", "uc:ai_ask"),
            ("actor:support_admin", "uc:support_open"),
            ("actor:shipping", "uc:shipment_timeline"),
        ],
        "includes": [
            ("product_detail", "view_reviews"),
            ("place_order", "checkout_prepare"),
            ("profile_order_detail", "shipment_timeline"),
            ("ai_ask", "ai_products"),
        ],
    },
    {
        "name": "02 Xác thực và tài khoản",
        "boundary": (230, 55, 850, 820),
        "actors": {
            "guest": (35, 150, ACTOR_W, ACTOR_H),
            "customer": (35, 430, ACTOR_W, ACTOR_H),
            "firebase": (1130, 175, 175, ACTOR_H),
        },
        "usecases": {
            "register": (275, 95, UC_W, UC_H),
            "login": (525, 95, UC_W, UC_H),
            "forgot_password": (775, 95, UC_W, UC_H),
            "login_email": (275, 185, UC_W, UC_H),
            "login_social": (525, 185, UC_W, UC_H),
            "login_phone": (775, 185, UC_W, UC_H),
            "login_magic": (525, 275, UC_W, UC_H),
            "firebase_sync": (775, 275, UC_W, UC_H),
            "logout": (275, 390, UC_W, UC_H),
            "profile": (525, 390, UC_W, UC_H),
            "profile_overview": (775, 390, UC_W, UC_H),
            "profile_history": (275, 485, UC_W, UC_H),
            "profile_order_detail": (525, 485, UC_W, UC_H),
            "profile_notifications": (775, 485, UC_W, UC_H),
            "address_book": (275, 600, UC_W, UC_H),
            "add_address": (525, 600, UC_W, UC_H),
            "set_default_address": (775, 600, UC_W, UC_H),
            "profile_favorites": (525, 720, UC_W, UC_H),
            "wishlist_remove": (775, 720, UC_W, UC_H),
        },
        "generalizations": [("actor:customer", "actor:guest")],
        "associations": [
            ("actor:guest", "uc:register"),
            ("actor:guest", "uc:login"),
            ("actor:guest", "uc:forgot_password"),
            ("actor:customer", "uc:logout"),
            ("actor:customer", "uc:profile"),
            ("actor:customer", "uc:profile_order_detail"),
            ("actor:customer", "uc:address_book"),
            ("actor:customer", "uc:profile_favorites"),
            ("actor:firebase", "uc:login_email"),
            ("actor:firebase", "uc:login_social"),
            ("actor:firebase", "uc:login_phone"),
            ("actor:firebase", "uc:login_magic"),
            ("actor:firebase", "uc:firebase_sync"),
            ("actor:firebase", "uc:forgot_password"),
        ],
        "includes": [
            ("register", "firebase_sync"),
            ("login", "firebase_sync"),
            ("profile", "profile_overview"),
            ("profile", "profile_history"),
            ("profile", "profile_notifications"),
            ("profile", "address_book"),
            ("address_book", "add_address"),
            ("address_book", "set_default_address"),
            ("profile_favorites", "wishlist_remove"),
        ],
        "extends": [
            ("login_email", "login"),
            ("login_social", "login"),
            ("login_phone", "login"),
            ("login_magic", "login"),
        ],
    },
    {
        "name": "03 Catalog, tìm kiếm và chi tiết sản phẩm",
        "boundary": (230, 45, 865, 900),
        "actors": {
            "guest": (35, 185, ACTOR_W, ACTOR_H),
            "customer": (35, 540, ACTOR_W, ACTOR_H),
        },
        "usecases": {
            "home": (275, 90, UC_W, UC_H),
            "view_campaigns": (525, 90, UC_W, UC_H),
            "home_sections": (775, 90, UC_W, UC_H),
            "browse_menu": (275, 180, UC_W, UC_H),
            "accessory_directory": (525, 180, UC_W, UC_H),
            "search": (775, 180, UC_W, UC_H),
            "search_suggest": (275, 270, UC_W, UC_H),
            "search_history": (525, 270, UC_W, UC_H),
            "search_results": (775, 270, UC_W, UC_H),
            "search_filter_sort": (525, 360, UC_W, UC_H),
            "catalog": (275, 455, UC_W, UC_H),
            "catalog_sectioned": (525, 455, UC_W, UC_H),
            "catalog_filters": (775, 455, UC_W, UC_H),
            "catalog_sort": (275, 545, UC_W, UC_H),
            "catalog_hot_sale": (525, 545, UC_W, UC_H),
            "catalog_section_products": (775, 545, UC_W, UC_H),
            "product_detail": (275, 660, UC_W, UC_H),
            "choose_variant": (525, 660, UC_W, UC_H),
            "view_gallery": (775, 660, UC_W, UC_H),
            "view_specs": (275, 750, UC_W, UC_H),
            "view_reviews": (525, 750, UC_W, UC_H),
            "view_related": (775, 750, UC_W, UC_H),
            "view_upsell": (275, 840, UC_W, UC_H),
            "view_qa": (525, 840, UC_W, UC_H),
            "cart_add": (775, 840, UC_W, UC_H),
        },
        "generalizations": [("actor:customer", "actor:guest")],
        "associations": [
            ("actor:guest", "uc:home"),
            ("actor:guest", "uc:browse_menu"),
            ("actor:guest", "uc:search"),
            ("actor:guest", "uc:catalog"),
            ("actor:guest", "uc:product_detail"),
            ("actor:customer", "uc:cart_add"),
            ("actor:customer", "uc:wishlist_toggle"),
        ],
        "includes": [
            ("home", "view_campaigns"),
            ("home", "home_sections"),
            ("home", "accessory_directory"),
            ("search", "search_suggest"),
            ("search", "search_results"),
            ("search", "search_history"),
            ("search_results", "search_filter_sort"),
            ("catalog", "catalog_filters"),
            ("catalog", "catalog_sort"),
            ("catalog", "catalog_hot_sale"),
            ("catalog_sectioned", "catalog_section_products"),
            ("product_detail", "choose_variant"),
            ("product_detail", "view_gallery"),
            ("product_detail", "view_specs"),
            ("product_detail", "view_reviews"),
            ("product_detail", "view_related"),
            ("product_detail", "view_upsell"),
            ("product_detail", "view_qa"),
            ("cart_add", "cart_validate"),
        ],
    },
    {
        "name": "04 Giỏ hàng và checkout",
        "boundary": (230, 45, 865, 900),
        "actors": {
            "guest": (35, 160, ACTOR_W, ACTOR_H),
            "customer": (35, 520, ACTOR_W, ACTOR_H),
            "province_api": (1135, 590, 175, ACTOR_H),
        },
        "usecases": {
            "cart_view": (275, 90, UC_W, UC_H),
            "cart_count": (525, 90, UC_W, UC_H),
            "cart_add": (775, 90, UC_W, UC_H),
            "cart_buy_now": (275, 185, UC_W, UC_H),
            "cart_update_qty": (525, 185, UC_W, UC_H),
            "cart_remove": (775, 185, UC_W, UC_H),
            "cart_remove_selected": (275, 280, UC_W, UC_H),
            "cart_select_checkout": (525, 280, UC_W, UC_H),
            "cart_recommendations": (775, 280, UC_W, UC_H),
            "cart_persist": (275, 375, UC_W, UC_H),
            "cart_validate": (525, 375, UC_W, UC_H),
            "checkout_prepare": (775, 375, UC_W, UC_H),
            "checkout_page": (275, 510, UC_W, UC_H),
            "checkout_default_address": (525, 510, UC_W, UC_H),
            "checkout_shipping": (775, 510, UC_W, UC_H),
            "checkout_location": (275, 605, UC_W, UC_H),
            "checkout_payment_method": (525, 605, UC_W, UC_H),
            "checkout_vouchers": (775, 605, UC_W, UC_H),
            "checkout_apply_voucher": (275, 700, UC_W, UC_H),
            "checkout_validate_voucher": (525, 700, UC_W, UC_H),
            "place_order": (775, 700, UC_W, UC_H),
            "create_order": (525, 805, UC_W, UC_H),
        },
        "generalizations": [("actor:customer", "actor:guest")],
        "associations": [
            ("actor:guest", "uc:cart_view"),
            ("actor:guest", "uc:cart_add"),
            ("actor:guest", "uc:cart_buy_now"),
            ("actor:customer", "uc:cart_persist"),
            ("actor:customer", "uc:checkout_page"),
            ("actor:customer", "uc:place_order"),
            ("actor:province_api", "uc:checkout_location"),
        ],
        "includes": [
            ("cart_view", "cart_count"),
            ("cart_add", "cart_validate"),
            ("cart_buy_now", "cart_validate"),
            ("cart_buy_now", "checkout_prepare"),
            ("cart_update_qty", "cart_persist"),
            ("cart_remove", "cart_persist"),
            ("cart_remove_selected", "cart_persist"),
            ("checkout_prepare", "cart_select_checkout"),
            ("checkout_prepare", "cart_validate"),
            ("checkout_prepare", "cart_recommendations"),
            ("checkout_page", "checkout_shipping"),
            ("checkout_page", "checkout_payment_method"),
            ("checkout_page", "checkout_vouchers"),
            ("checkout_shipping", "checkout_location"),
            ("checkout_shipping", "checkout_default_address"),
            ("checkout_apply_voucher", "checkout_validate_voucher"),
            ("place_order", "checkout_validate_voucher"),
            ("place_order", "create_order"),
        ],
    },
    {
        "name": "05 Thanh toán, đơn hàng và đánh giá",
        "boundary": (230, 45, 880, 900),
        "actors": {
            "customer": (35, 300, ACTOR_W, ACTOR_H),
            "momo": (1160, 95, 175, ACTOR_H),
            "vnpay": (1160, 225, 175, ACTOR_H),
            "sepay": (1160, 360, 175, ACTOR_H),
            "sepay_webhook": (1160, 505, 175, ACTOR_H),
            "shipping": (1160, 700, 175, ACTOR_H),
        },
        "usecases": {
            "pay_cod": (275, 90, UC_W, UC_H),
            "pay_momo": (525, 90, UC_W, UC_H),
            "pay_vnpay": (775, 90, UC_W, UC_H),
            "pay_sepay": (275, 185, UC_W, UC_H),
            "payment_create_url": (525, 185, UC_W, UC_H),
            "payment_return": (775, 185, UC_W, UC_H),
            "payment_ipn": (275, 280, UC_W, UC_H),
            "payment_result": (525, 280, UC_W, UC_H),
            "payment_confirm": (775, 280, UC_W, UC_H),
            "payment_cancel_failed": (525, 375, UC_W, UC_H),
            "sepay_instruction": (275, 485, UC_W, UC_H),
            "sepay_copy": (525, 485, UC_W, UC_H),
            "sepay_status": (775, 485, UC_W, UC_H),
            "sepay_cancel": (275, 580, UC_W, UC_H),
            "sepay_webhook_receive": (525, 580, UC_W, UC_H),
            "checkout_success": (775, 580, UC_W, UC_H),
            "profile_history": (275, 710, UC_W, UC_H),
            "profile_order_detail": (525, 710, UC_W, UC_H),
            "order_status": (775, 710, UC_W, UC_H),
            "shipment_timeline": (275, 805, UC_W, UC_H),
            "order_support_info": (525, 805, UC_W, UC_H),
            "review_after_purchase": (775, 805, UC_W, UC_H),
            "submit_review": (525, 900, UC_W, UC_H),
        },
        "associations": [
            ("actor:customer", "uc:pay_cod"),
            ("actor:customer", "uc:pay_momo"),
            ("actor:customer", "uc:pay_vnpay"),
            ("actor:customer", "uc:pay_sepay"),
            ("actor:customer", "uc:profile_history"),
            ("actor:customer", "uc:profile_order_detail"),
            ("actor:customer", "uc:review_after_purchase"),
            ("actor:momo", "uc:pay_momo"),
            ("actor:momo", "uc:payment_return"),
            ("actor:momo", "uc:payment_ipn"),
            ("actor:vnpay", "uc:pay_vnpay"),
            ("actor:vnpay", "uc:payment_return"),
            ("actor:vnpay", "uc:payment_ipn"),
            ("actor:sepay", "uc:pay_sepay"),
            ("actor:sepay", "uc:sepay_status"),
            ("actor:sepay_webhook", "uc:sepay_webhook_receive"),
            ("actor:shipping", "uc:shipment_timeline"),
        ],
        "includes": [
            ("pay_momo", "payment_create_url"),
            ("pay_vnpay", "payment_create_url"),
            ("pay_momo", "payment_return"),
            ("pay_vnpay", "payment_return"),
            ("payment_return", "payment_result"),
            ("payment_return", "payment_confirm"),
            ("payment_ipn", "payment_confirm"),
            ("payment_ipn", "payment_cancel_failed"),
            ("pay_sepay", "sepay_instruction"),
            ("pay_sepay", "sepay_status"),
            ("sepay_instruction", "sepay_copy"),
            ("sepay_webhook_receive", "payment_confirm"),
            ("profile_order_detail", "order_status"),
            ("profile_order_detail", "shipment_timeline"),
            ("profile_order_detail", "order_support_info"),
            ("review_after_purchase", "submit_review"),
        ],
        "extends": [
            ("payment_cancel_failed", "payment_result"),
            ("sepay_cancel", "pay_sepay"),
            ("checkout_success", "payment_confirm"),
        ],
    },
    {
        "name": "06 Hỗ trợ, AI và yêu thích",
        "boundary": (230, 45, 880, 820),
        "actors": {
            "guest": (35, 150, ACTOR_W, ACTOR_H),
            "customer": (35, 455, ACTOR_W, ACTOR_H),
            "support_admin": (1160, 105, 175, ACTOR_H),
            "realtime": (1160, 290, 175, ACTOR_H),
            "gemini": (1160, 560, 175, ACTOR_H),
        },
        "usecases": {
            "support_open": (275, 90, UC_W, UC_H),
            "chat_bootstrap": (525, 90, UC_W, UC_H),
            "support_login_prompt": (775, 90, UC_W, UC_H),
            "support_send": (275, 185, UC_W, UC_H),
            "realtime_connect": (525, 185, UC_W, UC_H),
            "support_http_fallback": (775, 185, UC_W, UC_H),
            "support_receive": (525, 280, UC_W, UC_H),
            "realtime_token": (775, 280, UC_W, UC_H),
            "ai_open": (275, 405, UC_W, UC_H),
            "ai_ask": (525, 405, UC_W, UC_H),
            "ai_history": (775, 405, UC_W, UC_H),
            "ai_reply": (275, 500, UC_W, UC_H),
            "ai_products": (525, 500, UC_W, UC_H),
            "ai_save": (775, 500, UC_W, UC_H),
            "ai_product_detail": (525, 595, UC_W, UC_H),
            "wishlist_status": (275, 715, UC_W, UC_H),
            "wishlist_toggle": (525, 715, UC_W, UC_H),
            "wishlist_remove": (775, 715, UC_W, UC_H),
        },
        "generalizations": [("actor:customer", "actor:guest")],
        "associations": [
            ("actor:guest", "uc:support_open"),
            ("actor:guest", "uc:ai_open"),
            ("actor:guest", "uc:ai_ask"),
            ("actor:customer", "uc:support_send"),
            ("actor:customer", "uc:wishlist_toggle"),
            ("actor:customer", "uc:wishlist_remove"),
            ("actor:support_admin", "uc:support_receive"),
            ("actor:realtime", "uc:realtime_connect"),
            ("actor:realtime", "uc:realtime_token"),
            ("actor:gemini", "uc:ai_ask"),
            ("actor:gemini", "uc:ai_reply"),
        ],
        "includes": [
            ("support_open", "chat_bootstrap"),
            ("support_open", "support_login_prompt"),
            ("support_send", "realtime_connect"),
            ("support_send", "realtime_token"),
            ("support_receive", "realtime_connect"),
            ("ai_open", "chat_bootstrap"),
            ("ai_ask", "ai_history"),
            ("ai_ask", "ai_reply"),
            ("ai_reply", "ai_products"),
            ("ai_products", "ai_product_detail"),
            ("ai_save", "realtime_token"),
            ("wishlist_toggle", "wishlist_status"),
        ],
        "extends": [
            ("support_http_fallback", "support_send"),
            ("ai_save", "ai_ask"),
            ("wishlist_remove", "wishlist_toggle"),
        ],
    },
]


def shifted_usecases(
    current: dict[str, tuple[int, int, int, int]],
    dy: int,
    added: dict[str, tuple[int, int, int, int]],
) -> dict[str, tuple[int, int, int, int]]:
    shifted = {
        key: (x, y + dy, width, height)
        for key, (x, y, width, height) in current.items()
    }
    return {**added, **shifted}


def _apply_overview_layering() -> None:
    DIAGRAMS[0].update({
        "name": "01 Tổng quan chức năng Web khách hàng",
        "boundary": (225, 45, 860, 620),
        "actors": {
            "guest": (35, 170, ACTOR_W, ACTOR_H),
            "customer": (35, 420, ACTOR_W, ACTOR_H),
            "firebase": (1125, 90, 175, ACTOR_H),
            "payment_gateway": (1125, 245, 175, ACTOR_H),
            "gemini": (1125, 400, 175, ACTOR_H),
            "support_admin": (1125, 555, 175, ACTOR_H),
            "shipping": (1125, 710, 175, ACTOR_H),
        },
        "usecases": {
            "shopping_overview": (315, 95, UC_W, UC_H),
            "account_overview": (660, 95, UC_W, UC_H),
            "wishlist_overview": (315, 220, UC_W, UC_H),
            "cart_overview": (660, 220, UC_W, UC_H),
            "checkout_overview": (315, 345, UC_W, UC_H),
            "order_aftercare_overview": (660, 345, UC_W, UC_H),
            "support_ai_overview": (488, 500, UC_W, UC_H),
        },
        "generalizations": [("actor:customer", "actor:guest")],
        "associations": [
            ("actor:guest", "uc:shopping_overview"),
            ("actor:guest", "uc:account_overview"),
            ("actor:guest", "uc:cart_overview"),
            ("actor:guest", "uc:support_ai_overview"),
            ("actor:customer", "uc:shopping_overview"),
            ("actor:customer", "uc:account_overview"),
            ("actor:customer", "uc:wishlist_overview"),
            ("actor:customer", "uc:cart_overview"),
            ("actor:customer", "uc:checkout_overview"),
            ("actor:customer", "uc:order_aftercare_overview"),
            ("actor:customer", "uc:support_ai_overview"),
            ("actor:firebase", "uc:account_overview"),
            ("actor:payment_gateway", "uc:checkout_overview"),
            ("actor:gemini", "uc:support_ai_overview"),
            ("actor:support_admin", "uc:support_ai_overview"),
            ("actor:shipping", "uc:order_aftercare_overview"),
        ],
        "includes": [],
        "extends": [],
    })

    DIAGRAMS[1].update({
        "name": "02 Chi tiết Xác thực và tài khoản",
        "boundary": (230, 45, 850, 900),
        "usecases": shifted_usecases(DIAGRAMS[1]["usecases"], 95, {
            "account_overview": (525, 80, UC_W, UC_H),
        }),
        "associations": [
            ("actor:guest", "uc:account_overview"),
            ("actor:customer", "uc:account_overview"),
            ("actor:firebase", "uc:register"),
            ("actor:firebase", "uc:login"),
            ("actor:firebase", "uc:forgot_password"),
            ("actor:firebase", "uc:firebase_sync"),
        ],
        "includes": [
            ("account_overview", "register"),
            ("account_overview", "login"),
            ("account_overview", "forgot_password"),
            ("account_overview", "logout"),
            ("account_overview", "profile"),
            *DIAGRAMS[1]["includes"],
        ],
    })

    DIAGRAMS[2].update({
        "name": "03 Chi tiết Catalog, tìm kiếm và sản phẩm",
        "boundary": (230, 45, 865, 1010),
        "usecases": shifted_usecases(DIAGRAMS[2]["usecases"], 95, {
            "shopping_overview": (525, 80, UC_W, UC_H),
        }),
        "associations": [
            ("actor:guest", "uc:shopping_overview"),
            ("actor:customer", "uc:shopping_overview"),
        ],
        "includes": [
            ("shopping_overview", "home"),
            ("shopping_overview", "browse_menu"),
            ("shopping_overview", "search"),
            ("shopping_overview", "catalog"),
            ("shopping_overview", "product_detail"),
            *DIAGRAMS[2]["includes"],
        ],
    })

    DIAGRAMS[3].update({
        "name": "04 Chi tiết Giỏ hàng và checkout",
        "boundary": (230, 45, 865, 1010),
        "usecases": shifted_usecases(DIAGRAMS[3]["usecases"], 95, {
            "cart_overview": (400, 80, UC_W, UC_H),
            "checkout_overview": (650, 80, UC_W, UC_H),
        }),
        "associations": [
            ("actor:guest", "uc:cart_overview"),
            ("actor:customer", "uc:cart_overview"),
            ("actor:customer", "uc:checkout_overview"),
            ("actor:province_api", "uc:checkout_location"),
        ],
        "includes": [
            ("cart_overview", "cart_view"),
            ("cart_overview", "cart_add"),
            ("cart_overview", "cart_buy_now"),
            ("cart_overview", "cart_update_qty"),
            ("cart_overview", "cart_remove"),
            ("cart_overview", "cart_remove_selected"),
            ("cart_overview", "cart_select_checkout"),
            ("checkout_overview", "checkout_prepare"),
            ("checkout_overview", "checkout_page"),
            ("checkout_overview", "place_order"),
            *DIAGRAMS[3]["includes"],
        ],
    })

    DIAGRAMS[4].update({
        "name": "05 Chi tiết Thanh toán, đơn hàng và đánh giá",
        "boundary": (230, 45, 880, 1100),
        "usecases": shifted_usecases(DIAGRAMS[4]["usecases"], 95, {
            "checkout_overview": (400, 80, UC_W, UC_H),
            "order_aftercare_overview": (650, 80, UC_W, UC_H),
        }),
        "associations": [
            ("actor:customer", "uc:checkout_overview"),
            ("actor:customer", "uc:order_aftercare_overview"),
            ("actor:momo", "uc:pay_momo"),
            ("actor:momo", "uc:payment_return"),
            ("actor:momo", "uc:payment_ipn"),
            ("actor:vnpay", "uc:pay_vnpay"),
            ("actor:vnpay", "uc:payment_return"),
            ("actor:vnpay", "uc:payment_ipn"),
            ("actor:sepay", "uc:pay_sepay"),
            ("actor:sepay", "uc:sepay_status"),
            ("actor:sepay_webhook", "uc:sepay_webhook_receive"),
            ("actor:shipping", "uc:shipment_timeline"),
        ],
        "includes": [
            ("checkout_overview", "pay_cod"),
            ("checkout_overview", "pay_momo"),
            ("checkout_overview", "pay_vnpay"),
            ("checkout_overview", "pay_sepay"),
            ("checkout_overview", "checkout_success"),
            ("order_aftercare_overview", "profile_history"),
            ("order_aftercare_overview", "profile_order_detail"),
            ("order_aftercare_overview", "review_after_purchase"),
            *DIAGRAMS[4]["includes"],
        ],
    })

    DIAGRAMS[5].update({
        "name": "06 Chi tiết Hỗ trợ, AI và yêu thích",
        "boundary": (230, 45, 880, 920),
        "usecases": shifted_usecases(DIAGRAMS[5]["usecases"], 95, {
            "support_ai_overview": (400, 80, UC_W, UC_H),
            "wishlist_overview": (650, 80, UC_W, UC_H),
        }),
        "associations": [
            ("actor:guest", "uc:support_ai_overview"),
            ("actor:customer", "uc:support_ai_overview"),
            ("actor:customer", "uc:wishlist_overview"),
            ("actor:support_admin", "uc:support_receive"),
            ("actor:realtime", "uc:realtime_connect"),
            ("actor:realtime", "uc:realtime_token"),
            ("actor:gemini", "uc:ai_ask"),
            ("actor:gemini", "uc:ai_reply"),
        ],
        "includes": [
            ("support_ai_overview", "support_open"),
            ("support_ai_overview", "support_send"),
            ("support_ai_overview", "support_receive"),
            ("support_ai_overview", "ai_open"),
            ("support_ai_overview", "ai_ask"),
            ("wishlist_overview", "wishlist_status"),
            ("wishlist_overview", "wishlist_toggle"),
            ("wishlist_overview", "wishlist_remove"),
            *DIAGRAMS[5]["includes"],
        ],
    })


_apply_overview_layering()


def build_project() -> dict:
    diagrams: list[dict] = []
    subjects: list[dict] = []
    for spec in DIAGRAMS:
        diagram, new_subjects = make_diagram(spec)
        diagrams.append(diagram)
        subjects.extend(new_subjects)

    model = {
        "_type": "UMLModel",
        "_id": MODEL_ID,
        "_parent": ref(PROJECT_ID),
        "name": "TechStore Customer Web",
        "ownedElements": [
            *diagrams,
            *subjects,
            *actors.values(),
            *use_cases.values(),
        ],
    }

    return {
        "_type": "Project",
        "_id": PROJECT_ID,
        "name": "TechStore Customer Web - Use Case",
        "ownedElements": [model],
    }


def build_markdown() -> str:
    actor_lines = "\n".join(f"- **{name}**" for name in ACTORS.values())
    usecase_lines = "\n".join(f"- {name}" for name in USE_CASES.values())
    diagram_lines = "\n".join(f"- `{spec['name']}`" for spec in DIAGRAMS)

    notes = "\n".join([
        "- Sơ đồ 01 là tổng quan cấp cao của web khách hàng; các sơ đồ 02-06 phân rã từng use case lớn thành use case con bằng quan hệ `include`.",
        "- Checkout bắt buộc đăng nhập; giỏ hàng có thể dùng session cho khách vãng lai rồi đồng bộ khi khách đăng nhập.",
        "- Đăng nhập/đăng ký dùng Firebase ở frontend, sau đó gọi `/Account/FirebaseSync` để tạo hoặc đồng bộ hồ sơ backend.",
        "- Các nút cập nhật thông tin cá nhân, đổi mật khẩu, liên kết tài khoản đang có giao diện nhưng chưa có action backend tương ứng trong project hiện tại.",
        "- Mapbox chọn vị trí trong checkout đang bị comment trong view, nên không đưa vào sơ đồ chính; địa chỉ đang dùng API tỉnh/huyện/xã `provinces.open-api.vn`.",
        "- Header có một số link tĩnh như `/stores`, `/orders/track`, `/deals`; chưa thấy controller xử lý trong web khách hàng nên chỉ xem như điều hướng/placeholder.",
    ])

    return f"""# Use Case Web Khách Hàng TechStore

File StarUML chính: `customer-web-usecase-staruml.mdj`

## Các sơ đồ trong StarUML
{diagram_lines}

## Actor
{actor_lines}

## Use case đã gom từ project
{usecase_lines}

## Ghi chú phạm vi
{notes}
"""


def main() -> None:
    project = build_project()
    MDJ_PATH.write_text(json.dumps(project, ensure_ascii=False, indent=2), encoding="utf-8")
    MD_PATH.write_text(build_markdown(), encoding="utf-8")


if __name__ == "__main__":
    main()
