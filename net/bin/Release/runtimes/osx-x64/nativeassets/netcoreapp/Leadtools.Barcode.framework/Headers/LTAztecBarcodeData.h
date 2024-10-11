// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.
// All Rights Reserved.
// *************************************************************
//
//  LTAztecBarcodeData.h
//  Leadtools.Barcode
//

#import <Leadtools.Barcode/LTBarcodeData.h>

NS_ASSUME_NONNULL_BEGIN

NS_CLASS_AVAILABLE(10_10, 8_0)
@interface LTAztecBarcodeData : LTBarcodeData <NSCopying>

@property (nonatomic, assign) LTBarcodeSymbology symbology;

@end

NS_ASSUME_NONNULL_END
