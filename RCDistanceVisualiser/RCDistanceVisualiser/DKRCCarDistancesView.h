//
//  DKRCCarDistancesView.h
//  RCDistanceVisualiser
//
//  Created by Daniel Kennett on 05/01/2013.
//  Copyright (c) 2013 Daniel Kennett. All rights reserved.
//

#import <Cocoa/Cocoa.h>

@interface DKRCCarDistancesView : NSView

@property (readwrite, nonatomic) NSUInteger maximumDrawDistance;

@property (readwrite, nonatomic) NSUInteger rearDistance;
@property (readwrite, nonatomic) NSUInteger frontLeftDistance;
@property (readwrite, nonatomic) NSUInteger frontRightDistance;
@property (readwrite, nonatomic) NSUInteger frontMiddleDistance;

@end
